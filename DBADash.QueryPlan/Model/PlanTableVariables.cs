using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// The operators that read or write a table variable (or a table-valued parameter), and the
    /// user-defined table-valued functions a statement calls - the two places a plan's row counts
    /// come from something other than statistics.
    ///
    /// Captured from SQL Server 2025 at compatibility levels 110 to 170, which is what these rules
    /// rest on:
    /// - A table variable is an Object named as the query names it, [@t], with no database or schema.
    ///   So is a table-valued parameter, which the ParameterList doesn't list, so the two can't be told
    ///   apart; a parameter's rows are known when the procedure compiles, at any level.
    /// - A read of one carries TableCardinality: the rows the optimizer believed it held at compile
    ///   time.  Zero on a plan compiled before the table was filled - every estimated plan, and every
    ///   plan below compatibility level 150 that wasn't recompiled - which is costed for one row.
    /// - A statement that modifies one reports TableVariableTransactionsDoNotSupportParallelNestedTransaction
    ///   as its NonParallelPlanReason, ahead of any other reason (even the Express edition's).
    /// - A multi-statement function is usually a Table-valued function operator filling its return
    ///   table under a Sequence, then a scan of that table carrying the function's own name and alias.
    ///   Called with arguments from the outer query (CROSS APPLY with a column) it is a lone Table-valued
    ///   function operator on the inner side of a loop, which looks the same as a CLR function would.
    /// - Interleaved execution is flagged (IsInterleavedExecuted) on every run of the plan, reused from
    ///   cache or not, but only on run figures: a cached plan has no flag, just the function's real
    ///   count where the fixed guess of 100 (1 with the legacy estimator) would be.
    /// - Built-in functions - STRING_SPLIT, OPENJSON, GENERATE_SERIES, the DMVs - have no database on
    ///   their Object, and stream their rows from the Table-valued function operator itself.
    /// </summary>
    public static class PlanTableVariables
    {
        /// <summary>The reason code showplan gives a statement that modifies a table variable.</summary>
        public const string NonParallelPlanReason = "TableVariableTransactionsDoNotSupportParallelNestedTransaction";

        /// <summary>The statement's operators that read or write a table variable, in node order.</summary>
        public static IReadOnlyList<PlanOperator> OperatorsIn(PlanStatement statement) =>
            statement.Operators.Where(Touches).OrderBy(op => op.NodeId).ToList();

        public static bool Touches(PlanOperator op) => op.Objects.Any(o => o.IsTableVariable);

        /// <summary>True when the operator inserts, updates or deletes rows in a table variable.</summary>
        public static bool Modifies(PlanOperator op) =>
            op.Category == PlanOperatorCategory.DataModification && Touches(op);

        /// <summary>The table variables the operator touches, as the query names them - @t.</summary>
        public static IEnumerable<string> NamesOn(PlanOperator op) =>
            op.Objects.Where(o => o.IsTableVariable).Select(o => o.Table!);

        /// <summary>
        /// True for a built-in table-valued function - a DMV such as SYSSESSIONS, or STRING_SPLIT,
        /// OPENJSON and the like - which showplan names with no database.
        /// </summary>
        public static bool IsBuiltInFunction(PlanOperator op) =>
            op.Kind == PlanOperatorKind.TableValuedFunction && string.IsNullOrEmpty(op.PrimaryObject?.Database);

        /// <summary>The built-in table-valued functions the statement reads, in node order.</summary>
        public static IReadOnlyList<PlanOperator> BuiltInFunctionsIn(PlanStatement statement) =>
            statement.Operators.Where(IsBuiltInFunction).OrderBy(op => op.NodeId).ToList();

        /// <summary>The user-defined table-valued functions the statement calls, in node order.</summary>
        public static IReadOnlyList<PlanFunctionCall> FunctionsIn(PlanStatement statement) =>
            statement.Operators
                .Where(op => op.Kind == PlanOperatorKind.TableValuedFunction && !IsBuiltInFunction(op))
                .OrderBy(op => op.NodeId)
                .Select(function => new PlanFunctionCall(
                    function,
                    statement.Operators
                        .Where(op => op != function && op.Objects.Any(o => SameCall(o, function.PrimaryObject!)))
                        .OrderBy(op => op.NodeId)
                        .ToList()))
                .ToList();

        /// <summary>The call a function's operator or a reader of its return table belongs to, or null.</summary>
        public static PlanFunctionCall? FunctionCallFor(PlanOperator op, PlanStatement statement) =>
            FunctionsIn(statement).FirstOrDefault(call => call.Function == op || call.Readers.Contains(op));

        /// <summary>The same function under the same alias: a query calling it twice reads two tables.</summary>
        private static bool SameCall(PlanObjectReference a, PlanObjectReference b) =>
            string.Equals(a.Database, b.Database, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.Schema, b.Schema, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.Table, b.Table, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.Alias, b.Alias, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>One call to a user-defined table-valued function - see <see cref="PlanTableVariables.FunctionsIn"/>.</summary>
    public sealed class PlanFunctionCall
    {
        internal PlanFunctionCall(PlanOperator function, IReadOnlyList<PlanOperator> readers)
        {
            Function = function;
            Readers = readers;
        }

        /// <summary>The Table-valued function operator.</summary>
        public PlanOperator Function { get; }

        /// <summary>The operators reading the return table the function filled; empty when it streams its rows.</summary>
        public IReadOnlyList<PlanOperator> Readers { get; }

        /// <summary>schema.name, as a reader would write it.</summary>
        public string Name =>
            Function.PrimaryObject is { } target
                ? string.IsNullOrEmpty(target.Schema) ? target.Table ?? string.Empty : target.Schema + "." + target.Table
                : string.Empty;

        /// <summary>True when interleaved execution ran it before the rest of the plan was compiled.</summary>
        public bool IsInterleaved => Function.Runtime?.IsInterleavedExecuted == true;

        /// <summary>
        /// Known to be a multi-statement T-SQL function: it filled a return table, or was
        /// interleaved, which only multi-statement functions are.  False for a call that streams its
        /// rows, which could be either a multi-statement or a CLR function.
        /// </summary>
        public bool IsMultiStatement => Readers.Count > 0 || IsInterleaved;

        /// <summary>The operators the call is made of: the function, then the reads of its return table.</summary>
        public IReadOnlyList<PlanOperator> Operators => new[] { Function }.Concat(Readers).ToList();
    }
}
