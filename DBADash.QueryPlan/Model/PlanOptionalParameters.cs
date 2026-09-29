using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// An operator whose predicate is written to match whether or not a parameter was supplied, and
    /// the parameters it does that for.
    /// </summary>
    public sealed class PlanOptionalParameterUse
    {
        internal PlanOptionalParameterUse(PlanOperator op, IReadOnlyList<string> parameters, IReadOnlyList<string> defaulted)
        {
            Operator = op;
            Parameters = parameters;
            Defaulted = defaulted;
        }

        public PlanOperator Operator { get; }

        /// <summary>The parameters or variables, as the query names them - @Name, without brackets.</summary>
        public IReadOnlyList<string> Parameters { get; }

        /// <summary>
        /// Those of <see cref="Parameters"/> written as col = ISNULL(@p, col) or COALESCE(@p, col),
        /// which SQL Server 2025's optional parameter plan optimization does not recognize.
        /// </summary>
        public IReadOnlyList<string> Defaulted { get; }
    }

    /// <summary>
    /// Finding the catch-all query: WHERE (col = @p OR @p IS NULL), and its ISNULL and COALESCE
    /// spellings, compiled without OPTION (RECOMPILE).
    ///
    /// Read from the plan's predicates rather than the statement text, because the plan is what
    /// says whether it mattered.  OPTION (RECOMPILE) compiles each run for its own values, which are
    /// written into the plan in place of the parameters, and the conditions they switch off go with
    /// them - so on an actual plan the pattern only survives where the values were not embedded.
    /// WITH RECOMPILE on a procedure recompiles without embedding anything, and leaves the pattern in
    /// place, which is correct: it does not help.  An estimated plan has no values to embed and keeps
    /// the pattern even under the hint, which is why the statement text is checked for it as well.
    ///
    /// Showplan writes a parameter or local variable as [@Name] in a predicate, and the three
    /// spellings come out as "col=[@p] OR [@p] IS NULL" (either way round), "isnull([@p],col)" and
    /// "CASE WHEN [@p] IS NOT NULL THEN [@p] ELSE col END".
    /// </summary>
    public static class PlanOptionalParameters
    {
        private const string Parameter = @"\[(?<p>@[^\]]+)\]";

        /// <summary>
        /// Where a column starts: a bracketed name that isn't a parameter, or a table variable's
        /// column, which showplan writes as [@t].[col].
        /// </summary>
        private const string ColumnStart = @"(?:\[(?!@)|\[@[^\]]+\]\.\[)";

        private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

        /// <summary>
        /// (col = @p OR @p IS NULL), in either order.  "IS NULL" never matches IS NOT NULL.  These only
        /// find the IS NULL half, so a match counts only when the predicate also compares the parameter
        /// to a column - see <see cref="ComparedToAColumn"/>.
        /// </summary>
        private static readonly Regex[] NullTests =
        [
            new(Parameter + @"\s+IS\s+NULL\s+OR\b", Options),
            new(@"\bOR\s+\(?" + Parameter + @"\s+IS\s+NULL\b", Options)
        ];

        /// <summary>
        /// A whole column reference: [db].[schema].[table].[col], or a table variable's [@t].[col],
        /// either with showplan's "as [alias].[col]" after it.
        /// </summary>
        private const string ColumnName =
            @"(?:\[(?!@)[^\]]*\]|\[@[^\]]+\](?=\.))(?:\.\[[^\]]*\])*(?:\s+as\s+\[[^\]]*\](?:\.\[[^\]]*\])*)?";

        /// <summary>
        /// A column captured as c, so the other side of the comparison can be held to the same one.
        /// Not starting partway through a longer name, nor followed by more of one.
        /// </summary>
        private const string ThisColumn = @"(?<![\].])(?<c>" + ColumnName + ")";

        private const string SameColumn = @"\k<c>(?!\.|\s+as\b)";

        private const string EqualTo = @"\s*=\s*";

        /// <summary>
        /// Spellings that name the column themselves, so need no further check: col = ISNULL(@p, col)
        /// and col = COALESCE(@p, col), which showplan writes as the CASE it is, either way round.
        /// Only equality, and only against the same column: ISNULL(@p, 0) is a constant, and seeks,
        /// and col1 &gt; ISNULL(@p, col2) is not a condition that switches off when @p is NULL.
        /// </summary>
        private static readonly Regex[] ColumnDefaults =
        [
            new(ThisColumn + EqualTo + @"isnull\(" + Parameter + @",\s*" + SameColumn + @"\)", Options),
            new(@"\bisnull\(" + Parameter + @",\s*" + ThisColumn + @"\)" + EqualTo + SameColumn, Options),
            new(ThisColumn + EqualTo + CoalesceOf(SameColumn), Options),
            new(CoalesceOf(ThisColumn) + EqualTo + SameColumn, Options)
        ];

        private static string CoalesceOf(string column) =>
            @"\bCASE\s+WHEN\s+" + Parameter + @"\s+IS\s+NOT\s+NULL\s+THEN\s+\[\k<p>\]\s+ELSE\s+" + column + @"\s+END";

        private static readonly Regex AnyColumn = new(ColumnStart, Options);

        /// <summary>
        /// OPTION (...) with RECOMPILE among its hints.  Across anything but a statement terminator,
        /// because the other hints have brackets of their own - OPTIMIZE FOR (@p = 1).
        /// </summary>
        private static readonly Regex RecompileHint =
            new(@"\bOPTION\s*\([^;]*\bRECOMPILE\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// The hint SQL Server 2025's optional parameter plan optimization adds to a statement's
        /// text when it compiles a plan for one variant of which parameters are NULL - one
        /// optional_predicate(@p IS NULL) for each parameter the variant was chosen by.
        /// </summary>
        private static readonly Regex OptionalPredicate =
            new(@"\boptional_predicate\s*\(\s*(?<p>@[^\s)]+)\s+IS\s+NULL\s*\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>The parameters <paramref name="predicate"/> treats as optional, in the order found.</summary>
        public static IReadOnlyList<string> InPredicate(string? predicate)
        {
            if (string.IsNullOrEmpty(predicate)) return [];

            return NullTests
                .SelectMany(pattern => pattern.Matches(predicate!).Cast<Match>())
                .Where(match => ComparedToAColumn(predicate!, match.Groups["p"].Value))
                .Concat(ColumnDefaults.SelectMany(pattern => pattern.Matches(predicate!).Cast<Match>()))
                .OrderBy(match => match.Index)
                .Select(match => match.Groups["p"].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The parameters <paramref name="predicate"/> treats as optional with ISNULL or COALESCE, in
        /// the order found.  SQL Server 2025 compiles no optional parameter plan variants for these
        /// spellings, only for (col = @p OR @p IS NULL).
        /// </summary>
        public static IReadOnlyList<string> DefaultedInPredicate(string? predicate)
        {
            if (string.IsNullOrEmpty(predicate)) return [];

            return ColumnDefaults
                .SelectMany(pattern => pattern.Matches(predicate!).Cast<Match>())
                .OrderBy(match => match.Index)
                .Select(match => match.Groups["p"].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Whether a parameter found in an IS NULL test is used again, in a predicate that reads a
        /// column.  Without that it only switches something on or off - a startup filter's
        /// "[@A] IS NULL OR [@B] IS NULL" - and reads no rows that a seek could have avoided.
        /// </summary>
        private static bool ComparedToAColumn(string predicate, string parameter) =>
            Regex.Matches(predicate, @"\[" + Regex.Escape(parameter) + @"\](?!\.)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count > 1
            && AnyColumn.IsMatch(predicate);

        /// <summary>
        /// True when the statement asks for OPTION (RECOMPILE).  Comments and string literals are
        /// ignored, so "-- TODO: OPTION (RECOMPILE)" is not taken for the hint.
        /// </summary>
        public static bool HasRecompileHint(string? statementText) =>
            !string.IsNullOrEmpty(statementText) && RecompileHint.IsMatch(WithoutCommentsOrStrings(statementText!));

        /// <summary>
        /// <paramref name="text"/> with comments and string literals each replaced by a space.
        /// Bracketed and quoted identifiers are kept as they are, so "--" inside one is not a comment.
        /// T-SQL block comments nest.
        /// </summary>
        internal static string WithoutCommentsOrStrings(string text)
        {
            var result = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var next = i + 1 < text.Length ? text[i + 1] : '\0';

                if (c == '-' && next == '-')
                {
                    while (i < text.Length && text[i] != '\n') i++;
                    result.Append(' ');
                }
                else if (c == '/' && next == '*')
                {
                    var depth = 0;
                    do
                    {
                        if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '*') { depth++; i += 2; }
                        else if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '/') { depth--; i += 2; }
                        else i++;
                    } while (depth > 0 && i < text.Length);
                    i--;
                    result.Append(' ');
                }
                else if (c == '\'')
                {
                    i = ClosingQuote(text, i, '\'');
                    result.Append(' ');
                }
                else if (c is '[' or '"')
                {
                    var end = Math.Min(ClosingQuote(text, i, c == '[' ? ']' : '"'), text.Length - 1);
                    result.Append(text, i, end - i + 1);
                    i = end;
                }
                else result.Append(c);
            }
            return result.ToString();
        }

        /// <summary>
        /// The index of the character closing the quote opened at <paramref name="open"/>, where a
        /// doubled closing character is an escaped one.  The end of the text when it is not closed.
        /// </summary>
        private static int ClosingQuote(string text, int open, char close)
        {
            var i = open + 1;
            while (i < text.Length)
            {
                if (text[i] != close) i++;
                else if (i + 1 < text.Length && text[i + 1] == close) i += 2;
                else return i;
            }
            return text.Length;
        }

        /// <summary>
        /// The parameters SQL Server 2025's optional parameter plan optimization chose this plan by,
        /// when it is one of that optimization's variants.  Empty when it is not.
        /// </summary>
        public static IReadOnlyList<string> OptimizedBy(string? statementText)
        {
            if (string.IsNullOrEmpty(statementText)) return [];

            return OptionalPredicate.Matches(statementText!).Cast<Match>()
                .Select(match => match.Groups["p"].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Every operator in the statement with an optional parameter predicate, in node order.
        /// Empty when there are none, or when the statement has OPTION (RECOMPILE) - see the class
        /// comment for why that is enough.
        /// </summary>
        public static IReadOnlyList<PlanOptionalParameterUse> In(PlanStatement statement)
        {
            if (HasRecompileHint(statement.StatementText)) return [];

            return statement.Operators
                .Select(Use)
                .OfType<PlanOptionalParameterUse>()
                .OrderBy(use => use.Operator.NodeId)
                .ToList();
        }

        /// <summary>
        /// <paramref name="op"/>'s optional parameter predicate, as <see cref="In"/> would report it.
        /// Null when it has none, or when the statement has OPTION (RECOMPILE).
        /// </summary>
        public static PlanOptionalParameterUse? On(PlanOperator op, PlanStatement statement) =>
            HasRecompileHint(statement.StatementText) ? null : Use(op);

        private static PlanOptionalParameterUse? Use(PlanOperator op) =>
            InPredicate(op.Predicate) is { Count: > 0 } parameters
                ? new PlanOptionalParameterUse(op, parameters, DefaultedInPredicate(op.Predicate))
                : null;
    }
}
