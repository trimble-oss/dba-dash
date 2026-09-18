using System.Globalization;
using System.Linq;
using System.Text;

namespace DBADash.QueryPlan.Model
{
    /// <summary>Which of a parameter's two values a script uses.</summary>
    public enum PlanParameterValues
    {
        /// <summary>The value this execution ran with.  Only on an actual plan.</summary>
        Runtime,

        /// <summary>The value the plan was compiled for.</summary>
        Compiled
    }

    /// <summary>
    /// T-SQL written from a plan, for the reader to run or adapt: the missing indexes it asked for,
    /// and its parameters as variables, to re-run the statement the way this execution ran.
    /// </summary>
    public static class PlanScripts
    {
        /// <summary>
        /// Every missing index for the statement as a CREATE INDEX statement, best first, each with
        /// what the optimiser expected of it.  Empty when there are none.
        /// </summary>
        public static string MissingIndexes(PlanStatement statement)
        {
            if (statement.MissingIndexes.Count == 0) return string.Empty;

            // The suggestions are the optimiser's view of one statement, made without looking at the
            // indexes the table already has or what another index costs to maintain - worth saying
            // above a script that is one keypress from being run.
            var script = new StringBuilder()
                .AppendLine("-- Missing indexes the optimizer suggested for this statement, highest estimated impact first.")
                .AppendLine("-- Each estimate is for this statement alone. Review them against the table's existing indexes")
                .AppendLine("-- and the cost of maintaining another index before creating any. Key columns are listed")
                .AppendLine("-- equality columns first, then inequality columns; their order within each group is not tuned.");

            var number = 0;
            foreach (var index in statement.MissingIndexes.OrderByDescending(i => i.Impact))
            {
                number++;
                script.AppendLine()
                    .Append("-- ").Append(number.ToString(CultureInfo.InvariantCulture)).Append(". ").Append(index.QualifiedTableName)
                    .Append(": estimated to reduce this statement's cost by ")
                    .Append(index.Impact.ToString("0.#", CultureInfo.InvariantCulture)).AppendLine("%");

                AppendColumns(script, "Equality", index.EqualityColumns);
                AppendColumns(script, "Inequality", index.InequalityColumns);
                AppendColumns(script, "Include", index.IncludedColumns);

                if (statement.Operators.FirstOrDefault(op => statement.MissingIndexesFor(op).Contains(index)) is { } reader)
                {
                    script.Append("--    Read by ").Append(reader.DisplayName).Append(" (node ")
                        .Append(reader.NodeId.ToString(CultureInfo.InvariantCulture)).AppendLine(")");
                }

                script.AppendLine(index.CreateStatement);
            }

            return script.ToString();
        }

        /// <summary>True when the plan recorded the values this execution ran with - an actual plan.</summary>
        public static bool HasRuntimeValues(PlanStatement statement) =>
            statement.Parameters.Any(p => p.RuntimeValue is not null);

        /// <summary>True when any parameter ran with a different value than the plan was compiled for.</summary>
        public static bool RuntimeValuesDiffer(PlanStatement statement) =>
            statement.Parameters.Any(p => p.CompiledValueDiffers);

        /// <summary>
        /// The parameters as DECLARE statements, for the Parameters tab: the values this execution
        /// ran with - or, on an estimated plan, those it was compiled for.  Where the two differ, both
        /// sets, each explained, since which one reproduces a problem depends on the problem: the
        /// runtime values are what ran slowly, the compiled values are what the plan was built for.
        /// Empty when the plan has no parameters.
        /// </summary>
        public static string DeclareParameters(PlanStatement statement)
        {
            if (statement.Parameters.Count == 0) return string.Empty;
            if (!HasRuntimeValues(statement)) return DeclareParameters(statement, PlanParameterValues.Compiled);
            if (!RuntimeValuesDiffer(statement)) return DeclareParameters(statement, PlanParameterValues.Runtime);

            return new StringBuilder()
                .AppendLine("-- This execution ran with different parameter values from those the plan was compiled for.")
                .AppendLine("-- Both sets follow. Use one or the other: they declare the same variables.")
                .AppendLine()
                .Append(DeclareParameters(statement, PlanParameterValues.Runtime))
                .AppendLine()
                .Append(DeclareParameters(statement, PlanParameterValues.Compiled))
                .ToString();
        }

        /// <summary>
        /// The parameters as DECLARE statements holding one set of values, with the other beside any
        /// that differ.  Empty when the plan has no parameters.
        /// </summary>
        public static string DeclareParameters(PlanStatement statement, PlanParameterValues values)
        {
            if (statement.Parameters.Count == 0) return string.Empty;

            var runtime = values == PlanParameterValues.Runtime;

            var script = new StringBuilder();
            if (runtime)
            {
                script.AppendLine("-- Runtime values: the parameter values this execution ran with.");
            }
            else
            {
                script.AppendLine("-- Compiled values: the parameter values the plan was compiled for.")
                    .AppendLine("-- The optimizer built the plan, and made its estimates, for these values.");
            }

            foreach (var parameter in statement.Parameters)
            {
                var value = runtime ? parameter.RuntimeValue : parameter.CompiledValue;
                var notes = new System.Collections.Generic.List<string>();

                // Showplan records the type from SQL Server 2017.  Without it the variable still has to
                // be declared as something for the script to run.
                var type = parameter.DataType;
                if (string.IsNullOrEmpty(type))
                {
                    type = "sql_variant";
                    notes.Add("the plan does not record its type: change sql_variant to it");
                }

                if (value is null) notes.Add("no value recorded in the plan");

                if (parameter.CompiledValueDiffers)
                {
                    notes.Add(runtime
                        ? "compiled for " + Literal(parameter.CompiledValue!)
                        : "ran with " + Literal(parameter.RuntimeValue!));
                }

                script.Append("DECLARE ").Append(parameter.Name).Append(' ').Append(type);
                if (value is not null) script.Append(" = ").Append(Literal(value));
                script.Append(';');
                if (notes.Count > 0) script.Append(" -- ").Append(string.Join("; ", notes));
                script.AppendLine();
            }

            return script.ToString();
        }

        private static void AppendColumns(StringBuilder script, string usage, System.Collections.Generic.IReadOnlyList<string> columns)
        {
            if (columns.Count == 0) return;
            script.Append("--    ").Append(usage).Append(": ").AppendLine(string.Join(", ", columns));
        }

        /// <summary>
        /// A value as showplan wrote it, without the brackets it puts round numbers - (42) is 42.  It
        /// writes every value as a T-SQL literal already, strings quoted and prefixed, so nothing else
        /// needs changing.  Only a single pair enclosing the whole value is taken off: (1)+(2) keeps
        /// both of its.
        /// </summary>
        internal static string Literal(string value)
        {
            if (value.Length < 2 || value[0] != '(' || value[^1] != ')') return value;

            var depth = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == '(') depth++;
                else if (value[i] == ')') depth--;

                // Closed before the end: the first bracket does not enclose the whole value.
                if (depth == 0 && i < value.Length - 1) return value;
            }

            return value[1..^1];
        }
    }
}
