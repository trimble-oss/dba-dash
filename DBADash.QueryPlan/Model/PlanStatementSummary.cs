using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// One statement of a batch reduced to the figures worth comparing it by, for the list the
    /// viewer navigates statements with.
    ///
    /// A batch or a stored procedure plan can hold dozens of statements, and the question is almost
    /// always "which one": the expensive one, the slow one, the one with the spill.  Answering that
    /// from a dropdown means opening every statement in turn.  A sortable list with the answers
    /// already on it means sorting a column.
    ///
    /// Here rather than in the viewer so the figures are computed once, the same way, whoever shows
    /// them - and so they can be tested without a window.
    /// </summary>
    public sealed class PlanStatementSummary
    {
        /// <summary>
        /// Statement text beyond this is not shown in the list.  A statement that long cannot be
        /// read in a list row anyway, and a list cell holding a hundred kilobytes of generated SQL
        /// makes the grid slow to paint for no benefit.  The full text is on the Query tab.
        /// </summary>
        public const int MaxTextLength = 2000;

        private PlanStatementSummary(PlanStatement statement, int ordinal)
        {
            Statement = statement;
            Ordinal = ordinal;
        }

        public PlanStatement Statement { get; }

        /// <summary>Position in the batch, from 1, in showplan order.</summary>
        public int Ordinal { get; }

        /// <summary>
        /// The statement as one line of text: runs of whitespace, line breaks included, collapsed to
        /// single spaces, so a list row of a few lines shows as much of the SQL as possible rather
        /// than mostly indentation.
        /// </summary>
        public string Text { get; private set; } = string.Empty;

        /// <summary>The statement's share of the whole batch's estimated cost, 0 to 1.</summary>
        public double CostShare { get; private set; }

        public double EstimatedCost => Statement.StatementSubTreeCost;

        public double? EstimatedRows => Statement.StatementEstRows;

        /// <summary>
        /// Rows the statement actually returned - what the outermost operator produced.  Null on an
        /// estimated plan.
        /// </summary>
        public long? ActualRows { get; private set; }

        public long? ElapsedMs => Statement.QueryTimeStats?.ElapsedMs;

        public long? CpuMs => Statement.QueryTimeStats?.CpuMs;

        /// <summary>
        /// This statement's elapsed time as a share of the slowest statement's, 0 to 1.  Against the
        /// slowest rather than the total so the slow statement has a full bar - the reader is asking
        /// which one it is, not what fraction of the batch it was.
        /// </summary>
        public double ElapsedShare { get; private set; }

        public long? GrantedMemoryKb => Statement.MemoryGrant?.GrantedMemoryKb;

        public long? UsedMemoryKb => Statement.MemoryGrant?.MaxUsedMemoryKb;

        public int? DegreeOfParallelism => Statement.DegreeOfParallelism;

        public int OperatorCount { get; private set; }

        /// <summary>Warnings on the statement and all its operators.</summary>
        public int WarningCount { get; private set; }

        /// <summary>True when any of those warnings is a spill, a missing join predicate or similar.</summary>
        public bool HasCriticalWarning { get; private set; }

        public int MissingIndexCount => Statement.MissingIndexes.Count;

        /// <summary>The best impact among the missing index recommendations, or null when there are none.</summary>
        public double? BestMissingIndexImpact =>
            Statement.MissingIndexes.Count == 0 ? null : Statement.MissingIndexes.Max(i => i.Impact);

        /// <summary>
        /// The worst row estimate in the statement, as a multiple of at least one whichever way it
        /// went: 100 means some operator was out by a hundredfold, over or under.  Null on an
        /// estimated plan, where there is nothing to compare.
        /// </summary>
        public double? WorstEstimateError { get; private set; }

        /// <summary>True when the statement carries measurements rather than only estimates.</summary>
        public bool IsActual => Statement.IsActualPlan;

        /// <summary>Summaries for every statement in a plan, in order.</summary>
        public static IReadOnlyList<PlanStatementSummary> For(ExecutionPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);

            var totalCost = plan.Statements.Sum(s => s.StatementSubTreeCost);
            var slowest = plan.Statements.Max(s => s.QueryTimeStats?.ElapsedMs ?? 0);

            return plan.Statements
                .Select((statement, index) => Build(statement, index + 1, totalCost, slowest))
                .ToList();
        }

        private static PlanStatementSummary Build(PlanStatement statement, int ordinal, double totalCost, long slowest)
        {
            var operators = statement.Operators.ToList();
            var warnings = statement.AllWarnings.ToList();

            var text = string.IsNullOrWhiteSpace(statement.StatementText)
                ? string.Empty
                : PlanFormat.SingleLine(statement.StatementText, MaxTextLength);

            double? worstError = null;
            foreach (var op in operators)
            {
                if (op.RowEstimateError is not { } error) continue;

                if (worstError is null || error > worstError) worstError = error;
            }

            return new PlanStatementSummary(statement, ordinal)
            {
                Text = text,
                CostShare = totalCost > 0 ? statement.StatementSubTreeCost / totalCost : 0,
                ElapsedShare = slowest > 0 && statement.QueryTimeStats?.ElapsedMs is { } elapsed
                    ? elapsed / (double)slowest
                    : 0,
                ActualRows = statement.RootOperator?.ActualRows is { } rows ? (long)rows : null,
                OperatorCount = operators.Count,
                WarningCount = warnings.Count,
                HasCriticalWarning = warnings.Any(w => w.Severity == PlanWarningSeverity.Critical),
                WorstEstimateError = worstError
            };
        }

        public override string ToString() => Ordinal + ". " + Statement.Caption;
    }
}
