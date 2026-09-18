using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Interaction
{
    /// <summary>
    /// Builds the hover tooltip for a node.
    ///
    /// Deliberately a curated list rather than the operator's whole property tree.  The properties
    /// panel exists for the full detail; a tooltip that reproduced it would be a wall of text the
    /// reader has to search, which is slower than not having one.  What is here is what people
    /// actually hover to find out: how many rows, how far off the estimate was, what it cost, what
    /// it was filtering on, and what went wrong.
    /// </summary>
    public static class PlanTooltipBuilder
    {
        /// <summary>
        /// Predicates run to hundreds of characters, and some to thousands.  They wrap on the tooltip
        /// - see <see cref="PlanTooltipRow.Wraps"/> - but are still cut off here, so a generated
        /// predicate does not make the clipboard copy of a tooltip a page long.  The properties panel
        /// has the whole of it.
        /// </summary>
        public const int DefaultMaxPredicateLength = 600;

        /// <summary>
        /// The most output columns named on a tooltip.  A wide SELECT * outputs dozens; past this the
        /// list says how many more there are, and the properties panel lists them all.
        /// </summary>
        public const int MaxOutputColumns = 20;

        /// <summary>
        /// Said after a row count taken from an operator's input because SQL Server did not measure
        /// the operator itself, so an inferred figure is never passed off as a measured one.
        /// </summary>
        private const string InferredNote = "  (from its input, not measured)";

        public static PlanTooltip Build(
            PlanNode node,
            int maxPredicateLength = DefaultMaxPredicateLength,
            OperatorTimeMode timeMode = OperatorTimeMode.Own,
            bool includeDescription = true)
        {
            return node.Operator is { } op
                ? BuildForOperator(node, op, maxPredicateLength, timeMode, includeDescription)
                : BuildForStatement(node.Statement);
        }

        private static PlanTooltip BuildForOperator(
            PlanNode node,
            PlanOperator op,
            int maxPredicateLength,
            OperatorTimeMode timeMode,
            bool includeDescription)
        {
            var rows = new List<PlanTooltipRow>();

            if (!string.IsNullOrEmpty(op.LogicalOp) &&
                !string.Equals(op.LogicalOp, op.PhysicalOp, System.StringComparison.OrdinalIgnoreCase))
            {
                rows.Add(new PlanTooltipRow("Logical operation", op.LogicalOp));
            }

            if (node.IsCollapsed && node.HiddenDescendantCount > 0)
            {
                rows.Add(new PlanTooltipRow(
                    "Collapsed",
                    node.HiddenDescendantCount.ToString(CultureInfo.InvariantCulture) + " operators hidden"));

                // The node carries their warning marker, so it has to say whose it is.
                if (node.HiddenWarningCount > 0)
                {
                    rows.Add(new PlanTooltipRow(
                        "Hidden warnings",
                        node.HiddenWarningCount == 1
                            ? "1 hidden operator has warnings"
                            : node.HiddenWarningCount.ToString(CultureInfo.InvariantCulture) + " hidden operators have warnings",
                        true));
                }
            }

            // Actuals first when there are any: on an actual plan the measurements are what the
            // reader came for, and putting the optimiser's guesses above them buries the answer.
            if (op.Runtime is { } runtime)
            {
                rows.Add(new PlanTooltipRow("Actual rows", PlanFormat.Rows(runtime.ActualRows)));

                if (runtime.ActualRowsRead is { } read && read != runtime.ActualRows)
                {
                    rows.Add(new PlanTooltipRow("Rows read", PlanFormat.Rows(read)));
                }

                if (op.RowsDiscarded is { } discarded)
                {
                    rows.Add(new PlanTooltipRow("Rows discarded", PlanFormat.Rows(discarded), true));
                }

                rows.Add(new PlanTooltipRow("Estimated rows", PlanFormat.Rows(op.EstimateRowsAllExecutions ?? op.EstimateRows)));

                if (op.RowEstimateRatio is { } ratio)
                {
                    rows.Add(new PlanTooltipRow(
                        "Actual vs estimated",
                        PlanFormat.EstimateRatio(ratio),
                        ratio >= 10 || ratio <= 0.1));
                }
                else if (op.RowEstimateError is { } error)
                {
                    // There is no ratio to an estimate of zero, but rows arrived against it all the
                    // same - and the node is badged for it, so the tooltip has to say why.  Counted
                    // against one row, the way the badge and the arrow colour count it.
                    rows.Add(new PlanTooltipRow("Actual vs estimated", PlanFormat.EstimateRatio(error), error >= 10));
                }

                if (runtime.LogicalExecutions > 1)
                {
                    rows.Add(new PlanTooltipRow("Executions", PlanFormat.Rows(runtime.LogicalExecutions)));
                }

                var otherMode = timeMode == OperatorTimeMode.Own ? OperatorTimeMode.AsReported : OperatorTimeMode.Own;

                if (TimeRow("Elapsed time", op.ElapsedMs(timeMode), op.ElapsedMs(otherMode), timeMode) is { } elapsed)
                {
                    rows.Add(elapsed);
                }

                if (TimeRow("CPU time", op.CpuMs(timeMode), op.CpuMs(otherMode), timeMode) is { } cpu)
                {
                    rows.Add(cpu);
                }

                if (runtime.ActualLogicalReads is { } reads && reads > 0)
                {
                    rows.Add(new PlanTooltipRow("Logical reads", PlanFormat.Rows(reads)));
                }

                if (runtime.WorkerThreadCount > 1)
                {
                    var threads = runtime.WorkerThreadCount.ToString(CultureInfo.InvariantCulture);

                    // Skew is the number that explains a parallel plan running no faster than a
                    // serial one, and it is invisible in the totals.
                    if (runtime.ThreadSkew is { } skew)
                    {
                        threads += "  (skew " + skew.ToString("0.0", CultureInfo.InvariantCulture) + "x)";
                    }

                    rows.Add(new PlanTooltipRow("Threads", threads, runtime.ThreadSkew >= 2));
                }

                if (runtime.IsBatchMode) rows.Add(new PlanTooltipRow("Execution mode", "Batch"));
            }
            else
            {
                // An unmeasured Compute Scalar on an actual plan: its input's count is its own.
                if (op.ActualRows is { } inferred)
                {
                    rows.Add(new PlanTooltipRow("Actual rows", PlanFormat.Rows(inferred) + InferredNote));
                }

                rows.Add(new PlanTooltipRow("Estimated rows", PlanFormat.Rows(op.EstimateRows)));

                // The inner side of a loop: the estimate above is for one execution, and the arrow
                // carries all of them, so the tooltip has to say how many that is.
                if (op.EstimatedExecutions > 1)
                {
                    rows.Add(new PlanTooltipRow("Estimated executions", PlanFormat.Rows(op.EstimatedExecutions)));
                    rows.Add(new PlanTooltipRow("Estimated rows, all executions", PlanFormat.Rows(op.EstimatedTotalRows)));
                }

                if (op.EstimateRowsWithoutRowGoal is { } withoutRowGoal &&
                    withoutRowGoal > op.EstimateRows)
                {
                    rows.Add(new PlanTooltipRow(
                        "Estimated rows without row goal",
                        PlanFormat.Rows(withoutRowGoal)));
                }
            }

            // What the arrow carries in bytes, which is what memory grants and spills are sized by.
            // Shown with the row size because that is the half of the product nobody can see.
            if (op.AvgRowSize is > 0)
            {
                rows.Add(new PlanTooltipRow(
                    "Data size",
                    PlanFormat.Bytes(op.DataSizeForDisplay) + "  (" +
                    op.AvgRowSize.Value.ToString("N0", CultureInfo.InvariantCulture) + " B per row)"));
            }

            rows.Add(new PlanTooltipRow(
                "Operator cost",
                PlanFormat.Cost(op.OperatorCost) + "  (" + PlanFormat.Percent(op.CostPercent) + ")"));

            rows.Add(new PlanTooltipRow(
                "Subtree cost",
                PlanFormat.Cost(op.EstimatedTotalSubtreeCost) + "  (" + PlanFormat.Percent(op.SubtreeCostPercent) + ")"));

            if (op.SeekPredicate is { } seek)
            {
                rows.Add(new PlanTooltipRow("Seek predicate", PlanFormat.SingleLine(seek, maxPredicateLength), wraps: true));
            }

            if (op.Predicate is { } predicate)
            {
                // A residual predicate on a scan is rows read and thrown away, which is why it is
                // called out rather than left to the properties panel.
                rows.Add(new PlanTooltipRow(
                    "Predicate",
                    PlanFormat.SingleLine(predicate, maxPredicateLength),
                    op.RowsDiscarded is not null,
                    wraps: true));
            }

            foreach (var warning in op.Warnings)
            {
                rows.Add(new PlanTooltipRow(
                    warning.Title,
                    warning.Detail is null ? "Yes" : PlanFormat.SingleLine(warning.Detail, maxPredicateLength),
                    warning.Severity != PlanWarningSeverity.Information,
                    wraps: true));
            }

            if (node.Badges.HasFlag(PlanNodeBadges.MissingIndex))
            {
                var impact = node.Statement.MissingIndexesFor(op)
                    .Select(i => i.Impact)
                    .DefaultIfEmpty(0)
                    .Max();

                rows.Add(new PlanTooltipRow(
                    "Missing index",
                    "Estimated impact " + impact.ToString("0.#", CultureInfo.InvariantCulture) + "%",
                    true));
            }

            // Last, as SSMS has it: what the operator hands on is looked up rather than scanned for,
            // and it answers the question a wide lookup or sort raises - which columns it is carrying.
            if (op.OutputList.Count > 0)
            {
                rows.Add(new PlanTooltipRow(
                    "Output list",
                    PlanColumnReference.Describe(op.OutputList, MaxOutputColumns),
                    wraps: true));
            }

            return new PlanTooltip(op.DisplayName, op.PrimaryObject?.ToString(), rows, includeDescription ? PlanOperatorDescriptions.For(op) : null);
        }

        /// <summary>
        /// The tooltip for an arrow: what flowed along it and what was expected to, in rows and in
        /// bytes, whatever the arrow happens to be drawn by.  The label on the arrow stays one short
        /// figure; this is where both sit side by side.
        /// </summary>
        public static PlanTooltip BuildForEdge(PlanEdge edge)
        {
            var rows = new List<PlanTooltipRow>();
            var producer = edge.From.Operator;

            if (edge.ActualRows is { } actual)
            {
                rows.Add(new PlanTooltipRow(
                    "Actual rows",
                    PlanFormat.Rows(actual) + (edge.IsActualRowsInferred ? InferredNote : string.Empty)));
            }

            // The estimate for every execution the optimiser expected, with the per execution figure
            // it was built from where there was more than one - a loop's inner side estimated at one
            // row a time is not the same claim as one row.
            var estimated = PlanFormat.Rows(edge.EstimatedRows);
            if (producer is { EstimatedExecutions: > 1 })
            {
                estimated += "  (" + PlanFormat.Rows(producer.EstimateRows) + " × " +
                             PlanFormat.Rows(producer.EstimatedExecutions) + " executions)";
            }

            rows.Add(new PlanTooltipRow("Estimated rows", estimated));

            if (edge.ActualRows is { } measured && edge.EstimateAccuracy is { } accuracy)
            {
                // The same floor of one row the colour uses, so the words and the colour agree.
                var ratio = measured <= 0 ? 0 : Math.Max(measured, 1) / Math.Max(edge.EstimatedRows, 1);

                rows.Add(new PlanTooltipRow(
                    "Actual vs estimated",
                    PlanFormat.EstimateRatio(ratio),
                    accuracy != PlanEstimateAccuracy.Good));
            }

            if (edge.RowSize is { } rowSize)
            {
                rows.Add(new PlanTooltipRow("Row size", rowSize.ToString("N0", CultureInfo.InvariantCulture) + " B (estimated)"));

                if (edge.ActualDataSize is { } actualSize)
                {
                    rows.Add(new PlanTooltipRow("Actual data size", PlanFormat.Bytes(actualSize)));
                }

                rows.Add(new PlanTooltipRow("Estimated data size", PlanFormat.Bytes(edge.EstimatedDataSize ?? 0)));
            }

            return new PlanTooltip(edge.From.Title + " → " + edge.To.Title, edge.From.Subtitle, rows);
        }

        /// <summary>
        /// A time in the chosen mode, with the other mode's figure beside it when the two differ.
        /// The node shows one of them; the tooltip is where the reader checks the other, without
        /// switching the whole picture over.  Null when the time was not measured.
        /// </summary>
        private static PlanTooltipRow? TimeRow(string name, long? shown, long? other, OperatorTimeMode mode)
        {
            if (shown is not { } value) return null;

            var text = PlanFormat.Duration(value);

            if (other is { } alternative && alternative != value)
            {
                text += mode == OperatorTimeMode.Own
                    ? "  (" + PlanFormat.Duration(alternative) + " cumulative)"
                    : "  (" + PlanFormat.Duration(alternative) + " this node)";
            }

            return new PlanTooltipRow(name, text);
        }

        /// <summary>
        /// The statement root's tooltip, which is the whole statement's summary - the figures that
        /// belong to the query rather than to any one operator.
        /// </summary>
        private static PlanTooltip BuildForStatement(PlanStatement statement)
        {
            var rows = new List<PlanTooltipRow>();

            if (statement.QueryTimeStats is { } times)
            {
                if (times.ElapsedMs is { } elapsed) rows.Add(new PlanTooltipRow("Elapsed time", PlanFormat.Duration(elapsed)));
                if (times.CpuMs is { } cpu) rows.Add(new PlanTooltipRow("CPU time", PlanFormat.Duration(cpu)));
                if (times.UdfElapsedMs is { } udf && udf > 0)
                {
                    rows.Add(new PlanTooltipRow("Time in UDFs", PlanFormat.Duration(udf), true));
                }
            }

            rows.Add(new PlanTooltipRow("Estimated cost", PlanFormat.Cost(statement.StatementSubTreeCost)));

            if (statement.DegreeOfParallelism is { } dop)
            {
                rows.Add(new PlanTooltipRow("Degree of parallelism", dop.ToString(CultureInfo.InvariantCulture)));
            }

            if (statement.NonParallelPlanReason is { } reason)
            {
                rows.Add(new PlanTooltipRow("Non parallel plan reason", reason, wraps: true));
            }

            if (statement.MemoryGrant is { } grant)
            {
                if (grant.GrantedMemoryKb is { } granted)
                {
                    var value = PlanFormat.Kilobytes(granted);

                    if (grant.GrantUsedFraction is { } used)
                    {
                        value += "  (" + PlanFormat.Percent(used) + " used)";
                    }

                    rows.Add(new PlanTooltipRow("Memory grant", value, grant.GrantUsedFraction is < 0.25));
                }

                if (grant.GrantWaitTimeMs is > 0)
                {
                    rows.Add(new PlanTooltipRow("Grant wait", PlanFormat.Duration(grant.GrantWaitTimeMs.Value), true));
                }
            }

            if (statement.CompileTimeMs is { } compile)
            {
                rows.Add(new PlanTooltipRow("Compile time", PlanFormat.Duration(compile)));
            }

            if (statement.OptimisationLevel is { } level)
            {
                rows.Add(new PlanTooltipRow("Optimisation level", level));
            }

            if (statement.OptimisationEarlyAbortReason is { } abort)
            {
                rows.Add(new PlanTooltipRow("Optimiser stopped early", abort, true));
            }

            // Parameter sniffing in one line: a plan compiled for one value and run with another is
            // the first thing to check when one execution is slow and the next is not.  The values
            // themselves are on the Parameters tab.
            var differing = statement.Parameters.Count(p => p.CompiledValueDiffers);
            if (differing > 0)
            {
                rows.Add(new PlanTooltipRow(
                    "Parameters",
                    differing.ToString(CultureInfo.InvariantCulture) + " of " +
                    statement.Parameters.Count.ToString(CultureInfo.InvariantCulture) +
                    " ran with a different value than compiled for",
                    true));
            }

            // The top waits, not all of them: the tail of a wait list is noise, and the full list is
            // on the Waits tab.
            foreach (var wait in statement.WaitStats.Take(3))
            {
                rows.Add(new PlanTooltipRow(
                    "Wait: " + wait.WaitType,
                    PlanFormat.Duration(wait.WaitTimeMs)));
            }

            foreach (var warning in statement.Warnings)
            {
                rows.Add(new PlanTooltipRow(
                    warning.Title,
                    warning.Detail ?? "Yes",
                    warning.Severity != PlanWarningSeverity.Information,
                    wraps: true));
            }

            if (statement.MissingIndexes.Count > 0)
            {
                rows.Add(new PlanTooltipRow(
                    "Missing indexes",
                    statement.MissingIndexes.Count.ToString(CultureInfo.InvariantCulture) +
                    ", best impact " +
                    statement.MissingIndexes[0].Impact.ToString("0.#", CultureInfo.InvariantCulture) + "%",
                    true));
            }

            var title = string.IsNullOrWhiteSpace(statement.StatementType) ? "Query" : statement.StatementType;
            var subtitle = string.IsNullOrWhiteSpace(statement.StatementText)
                ? null
                : PlanFormat.SingleLine(statement.StatementText, 160);

            return new PlanTooltip(title, subtitle, rows);
        }
    }
}
