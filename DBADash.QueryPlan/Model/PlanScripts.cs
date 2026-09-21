using System;
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

            var script = new StringBuilder();
            AppendPreamble(script, one: false);

            var number = 0;
            foreach (var index in statement.MissingIndexes.OrderByDescending(i => i.Impact))
            {
                AppendMissingIndex(script, statement, index, ++number);
            }

            return script.ToString();
        }

        /// <summary>
        /// One missing index written the way the Missing Indexes tab writes it - the same notes above
        /// the same CREATE INDEX.
        ///
        /// What a grid row's link and an insight card's View T-SQL open, rather than the bare
        /// statement: the notes are the part that says what the index would cost and, on a temp table,
        /// that creating it this way costs the table its caching, and a reader who reached the script
        /// from a card has seen none of that.
        /// </summary>
        public static string MissingIndex(PlanStatement statement, PlanMissingIndex index)
        {
            var script = new StringBuilder();
            AppendPreamble(script, one: true);
            AppendMissingIndex(script, statement, index, null);
            return script.ToString();
        }

        /// <summary>
        /// The suggestions are the optimiser's view of one statement, made without looking at the
        /// indexes the table already has or what another index costs to maintain - worth saying above
        /// a script that is one keypress from being run.
        /// </summary>
        private static void AppendPreamble(StringBuilder script, bool one)
        {
            if (one)
            {
                script.AppendLine("/* A missing index the optimizer suggested for this statement.  The estimate is for this")
                    .AppendLine("   statement alone. Review it against the table's existing indexes and the cost of")
                    .AppendLine("   maintaining another index before creating it. Key columns are listed equality columns")
                    .AppendLine("   first, then inequality columns; their order within each group is not tuned.")
                    .AppendLine("*/");
                return;
            }

            script.AppendLine("/* Missing indexes the optimizer suggested for this statement, highest estimated impact first.")
                .AppendLine("   Each estimate is for this statement alone. Review them against the table's existing indexes")
                .AppendLine("   and the cost of maintaining another index before creating any. Key columns are listed")
                .AppendLine("   equality columns first, then inequality columns; their order within each group is not tuned.")
                .AppendLine("*/");
        }

        /// <summary>
        /// One recommendation: what the optimiser expected of it, where it would be read, and the
        /// statement that creates it.  Numbered only where there is more than one to tell apart.
        /// </summary>
        private static void AppendMissingIndex(StringBuilder script, PlanStatement statement, PlanMissingIndex index, int? number)
        {
            // Built on its own and wrapped afterwards, so that everything the plan supplied passes
            // through Commented on its way into the comment - see there for why that matters.
            var notes = new StringBuilder();
            if (number is { } position) notes.Append(position.ToString(CultureInfo.InvariantCulture)).Append(". ");

            notes.Append(index.QualifiedTableName)
                .Append(": estimated to reduce this statement's cost by ")
                .Append(index.Impact.ToString("0.#", CultureInfo.InvariantCulture)).AppendLine("%");

            AppendColumns(notes, "Equality", index.EqualityColumns);
            AppendColumns(notes, "Inequality", index.InequalityColumns);
            AppendColumns(notes, "Include", index.IncludedColumns);

            if (statement.Operators.FirstOrDefault(op => statement.MissingIndexesFor(op).Contains(index)) is { } reader)
            {
                notes.Append("   Read by ").Append(reader.DisplayName).Append(" (node ")
                    .Append(reader.NodeId.ToString(CultureInfo.InvariantCulture)).AppendLine(")");
            }

            AppendTempTableNote(notes, index);

            script.AppendLine().Append("/* ").Append(Commented(notes.ToString())).AppendLine("*/")
                .AppendLine(index.CreateStatement);
        }

        /// <summary>
        /// One expression written out for reading: where it is worked out and used, the definition
        /// itself, and - when it is built from other expressions - the whole thing with those written
        /// out in place.
        ///
        /// Laid out with <see cref="PlanFormat.ForReading"/> rather than left on one line, because a
        /// generated expression is nested several deep and a reader following one is reading it, not
        /// glancing at it.  Not runnable T-SQL and not meant to be: it is shown in the code viewer
        /// because that is where the rest of a plan's text is shown, and the highlighting helps.
        /// </summary>
        public static string Expression(PlanExpression expression)
        {
            var notes = new StringBuilder(expression.DisplayName).AppendLine();

            if (expression.DefinedBy is { } definedBy)
            {
                notes.Append("   Worked out by ").AppendLine(Describe(definedBy));
            }

            if (expression.UsedBy.Count > 0)
            {
                notes.Append("   Used by ")
                    .AppendLine(string.Join(", ", expression.UsedBy.Select(Describe)));
            }

            if (expression.References.Count > 0)
            {
                notes.Append("   Built from ").AppendLine(string.Join(", ", expression.References));
            }

            var script = new StringBuilder()
                .Append("/* ").Append(Commented(notes.ToString())).AppendLine("*/")
                .AppendLine(PlanFormat.ForReading(expression.Definition));

            if (expression.IsNested)
            {
                script.AppendLine()
                    .AppendLine("/* The same thing with every expression it refers to written out in place. */")
                    .AppendLine(PlanFormat.ForReading(expression.Expanded));
            }

            return script.ToString();
        }

        /// <summary>
        /// The plan's own values that <paramref name="text"/> refers to, as a note to go under it -
        /// or nothing when it refers to none.
        ///
        /// A predicate reading Expr1011 &gt; Expr1013 says nothing on its own, and the properties
        /// panel shows a value in full without the room, or the hit testing, for a link on each name:
        /// so whatever the text refers to is written underneath it instead.  Bracketed or bare, so
        /// the sort keys and group by lists are covered as well as the predicates.
        /// </summary>
        public static string ExpressionsUsedIn(PlanStatement? statement, string? text)
        {
            if (statement is null || string.IsNullOrEmpty(text)) return string.Empty;

            var used = PlanExpressions.NamesIn(text)
                .Select(statement.ExpressionNamed)
                .Where(expression => expression is not null)
                .Distinct()
                .ToList();

            if (used.Count == 0) return string.Empty;

            var notes = new StringBuilder("The values the plan works out that the text above refers to.").AppendLine();

            foreach (var expression in used)
            {
                notes.Append("   ").Append(expression!.Name).Append(" = ").AppendLine(expression.DefinitionOneLine);

                if (expression.IsNested) notes.Append("      in full: ").AppendLine(expression.ExpandedOneLine);
            }

            return Environment.NewLine + "/* " + Commented(notes.ToString()) + "*/" + Environment.NewLine;
        }

        /// <summary>An operator as a note names it: what it is, and which node, since a plan has several of each.</summary>
        private static string Describe(PlanOperator op) =>
            op.DisplayName + " (node " + op.NodeId.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>
        /// Text made safe to sit inside a block comment.
        ///
        /// The table and column names in these notes are whatever the plan carried, and a delimited
        /// identifier can hold anything - including the two characters that end a comment.  A table
        /// named so that its name ends one would leave the rest of the note running as T-SQL in the
        /// window of whoever copied the script, which is a long way from what they asked for.  The
        /// delimiters are the only thing a comment gives meaning to, so a space between the two
        /// characters is all it takes: nested comments mean an opening one is worth the same care.
        /// </summary>
        private static string Commented(string text) =>
            text.Replace("*/", "* /").Replace("/*", "/ *");

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
                .AppendLine("/* This execution ran with different parameter values from those the plan was compiled for.")
                .AppendLine("   Both sets follow. Use one or the other: they declare the same variables.")
                .AppendLine("*/")
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
                script.AppendLine("/* Runtime values: the parameter values this execution ran with. */");
            }
            else
            {
                script.AppendLine("/* Compiled values: the parameter values the plan was compiled for.")
                    .AppendLine("   The optimizer built the plan, and made its estimates, for these values.")
                    .AppendLine("*/");
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
                    // On one line: the note is a -- comment, and a value with a line break in it
                    // would put the rest of the note back in the reader's batch as T-SQL.
                    notes.Add(runtime
                        ? "compiled for " + PlanFormat.SingleLine(Literal(parameter.CompiledValue!), 200)
                        : "ran with " + PlanFormat.SingleLine(Literal(parameter.RuntimeValue!), 200));
                }

                script.Append("DECLARE ").Append(parameter.Name).Append(' ').Append(type);
                if (value is not null) script.Append(" = ").Append(Literal(value));
                script.Append(';');
                if (notes.Count > 0) script.Append(" -- ").Append(string.Join("; ", notes));
                script.AppendLine();
            }

            return script.ToString();
        }

        /// <summary>
        /// Why an index on a temp table is better declared on the CREATE TABLE, and the line to paste
        /// there.  The CREATE INDEX below it still runs, because the reader cannot always reach the
        /// CREATE TABLE - it may be in a procedure they do not own - but it should not be the first
        /// thing they reach for.
        /// </summary>
        private static void AppendTempTableNote(StringBuilder script, PlanMissingIndex index)
        {
            if (!index.IsTempTable) return;

            script.AppendLine()
                .Append("   ").Append(index.Table).AppendLine(" is a temp table.")
                .AppendLine("   Adding an index after the table exists stops SQL Server reusing a cached temp table")
                .AppendLine("   definition between executions of the procedure that creates it.  At high volume execution")
                .AppendLine("   this can cause contention.  Declared on the CREATE TABLE the caching is kept ")
                .AppendLine("   (SQL Server 2014 and later; 2016 and later for INCLUDE columns):")
                .AppendLine()
                .Append("     CREATE TABLE [").Append(index.Table).Append("] (<columns>, ")
                .Append(index.InlineIndexDefinition).AppendLine(");");
        }

        private static void AppendColumns(StringBuilder script, string usage, System.Collections.Generic.IReadOnlyList<string> columns)
        {
            if (columns.Count == 0) return;
            script.Append("   ").Append(usage).Append(": ").AppendLine(string.Join(", ", columns));
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
