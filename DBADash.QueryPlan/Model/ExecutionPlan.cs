using System.Collections.Generic;
using System.Linq;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// A parsed showplan document: one or more statements, each with its plan.
    ///
    /// A .sqlplan usually holds a single statement, but a batch, a stored procedure or a plan
    /// captured from the cache can hold many, and the viewer has to let the reader pick between
    /// them rather than silently showing the first.
    /// </summary>
    public sealed class ExecutionPlan
    {
        /// <summary>The showplan schema version, e.g. 1.539.</summary>
        public string? Version { get; internal set; }

        /// <summary>The SQL Server build that produced the plan, e.g. 16.0.4165.4.</summary>
        public string? Build { get; internal set; }

        /// <summary>
        /// Every statement in the document, in order, flattened out of the batch and conditional
        /// nesting showplan wraps them in.  <see cref="PlanStatement.NestingLevel"/> keeps the
        /// structure that flattening would otherwise lose.
        /// </summary>
        public IReadOnlyList<PlanStatement> Statements { get; internal set; } = [];

        /// <summary>
        /// The statement worth opening on: the most expensive one that actually has a plan.
        ///
        /// Not the first one.  A batch that starts with a few cheap statements and ends with the one
        /// that took a minute is the normal case, and opening on the cheap one means everybody's
        /// first action is to go hunting in a dropdown.
        /// </summary>
        public PlanStatement? PrimaryStatement =>
            Statements.Where(s => s.HasPlan).OrderByDescending(s => s.StatementSubTreeCost).FirstOrDefault()
            ?? Statements.FirstOrDefault();

        /// <summary>True when any statement carries actual measurements.</summary>
        public bool IsActualPlan => Statements.Any(s => s.IsActualPlan);

        /// <summary>Every statement that has a plan to draw.</summary>
        public IEnumerable<PlanStatement> StatementsWithPlans => Statements.Where(s => s.HasPlan);
    }
}
