using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// One value the plan works out for itself and gives a name to - Expr1011, and the rest.
    ///
    /// The optimiser names the results of its own arithmetic and hands the names around: a Compute
    /// Scalar defines Expr1011, a Filter three operators away tests it, and nothing in between says
    /// what it is.  That is the one thing a reader cannot get from the plan without hunting, because
    /// the definition and the use are in different operators - so every definition is collected once,
    /// here, and anything showing a name can say what it means.
    ///
    /// <see cref="Expanded"/> is the same expression with the names it refers to written out in
    /// place, because they nest: Expr1011 is often CONVERT_IMPLICIT of Expr1010, which is a column
    /// plus Expr1009, and only the whole thing says what the query is actually doing.
    ///
    /// One name can be defined more than once.  A parallel aggregate computes a value in a Compute
    /// Scalar and then carries it up through a local and a global Stream Aggregate, each of which
    /// defines the same name again as ANY of itself - so a plan can hold Expr1033 = the expression,
    /// and Expr1033 = ANY([Expr1033]) twice over.  Each definition gets its own entry, because each
    /// is something an operator does; what the name means is the definition that computes it, which
    /// is what <see cref="PlanStatement.ExpressionNamed"/> resolves to and what the ANY definitions
    /// expand to.
    /// </summary>
    public sealed class PlanExpression
    {
        internal PlanExpression(string name, string? qualifier, string definition, PlanOperator? definedBy)
        {
            Name = name;
            Qualifier = qualifier;
            Definition = definition;
            Expanded = definition;
            DefinedBy = definedBy;
        }

        /// <summary>The name on its own, as it is written inside brackets wherever it is used.</summary>
        public string Name { get; }

        /// <summary>
        /// The table or alias the name belongs to, where it is a real column rather than one of the
        /// optimiser's own.  Null for a generated name, which belongs to the plan rather than to any
        /// table.
        /// </summary>
        public string? Qualifier { get; }

        /// <summary>The expression itself, exactly as showplan wrote it.</summary>
        public string Definition { get; }

        /// <summary>
        /// The operator that works the value out.  Null only for a definition found outside any
        /// operator, which showplan does not currently produce.
        /// </summary>
        public PlanOperator? DefinedBy { get; internal set; }

        /// <summary>
        /// <see cref="Definition"/> with every name it refers to replaced by that name's own
        /// definition, and so on down.  The same as <see cref="Definition"/> when it refers to
        /// nothing - see <see cref="IsNested"/>.
        /// </summary>
        public string Expanded { get; internal set; }

        /// <summary>The names this expression is built from, in the order they appear in it.</summary>
        public IReadOnlyList<string> References { get; internal set; } = [];

        /// <summary>
        /// The operators that use the name, other than the one that defines it.  What answers "so
        /// what is it for", which is the question a reader has once they know what it means.
        /// </summary>
        public IReadOnlyList<PlanOperator> UsedBy { get; internal set; } = [];

        /// <summary>
        /// A name the optimiser invented rather than one of the query's own columns.  Only these are
        /// worth linking and expanding: a generated name means exactly one thing in the statement,
        /// where a column name means whatever the row it is in came from.
        /// </summary>
        public bool IsGenerated => Qualifier is null && PlanExpressions.IsGeneratedName(Name);

        /// <summary>True when the expression is built from others, so <see cref="Expanded"/> says more.</summary>
        public bool IsNested => !string.Equals(Expanded, Definition, StringComparison.Ordinal);

        /// <summary>The name as it is worth showing: qualified for a real column, bare for a generated one.</summary>
        public string DisplayName => Qualifier is null ? Name : Qualifier + "." + Name;

        /// <summary>The definition on one line and cut to a length a grid cell can carry.</summary>
        public string DefinitionOneLine => PlanFormat.SingleLine(Definition, PlanExpressions.MaxOneLineLength);

        /// <summary>The expansion on one line and cut the same way, or empty when it says nothing more.</summary>
        public string ExpandedOneLine =>
            IsNested ? PlanFormat.SingleLine(Expanded, PlanExpressions.MaxOneLineLength) : string.Empty;

        /// <summary>Where the value is worked out, for a list that has to say so in one column.</summary>
        public string DefinedByDescription => DefinedBy is { } op ? Describe(op) : string.Empty;

        /// <summary>
        /// Where it is used, for the same column's worth of room.  A name carried through half a
        /// plan is used by a dozen operators, and a cell naming all of them says less than one that
        /// names three and counts the rest.
        /// </summary>
        public string UsedByDescription =>
            PlanFormat.List(UsedBy.Select(Describe).ToList(), PlanExpressions.MaxNamedUsers);

        private static string Describe(PlanOperator op) =>
            op.DisplayName + " (node " + op.NodeId.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";

        public override string ToString() => DisplayName + " = " + Definition;
    }

    /// <summary>
    /// Collecting the expressions a statement defines, finding where they are referred to, and
    /// writing them out in full.
    ///
    /// Here rather than in the viewer so that what counts as a reference is one set of rules, shared
    /// by the list, the cards and anything else showing plan text - and so they are tested without a
    /// window.
    /// </summary>
    public static class PlanExpressions
    {
        /// <summary>
        /// How large an expansion is allowed to grow to before it stops going any deeper and leaves
        /// the rest as names.
        ///
        /// Expressions can refer to the same name more than once, so writing each one out in place
        /// multiplies rather than adds: a chain of a dozen, each using the one before it twice, would
        /// expand to something nobody will read and take a while doing it.  Once the text has grown
        /// past this the substitution stops, and whatever is left - including the part already in hand -
        /// is kept as it stands: this is a limit on how far down the rabbit hole to go, not a hard cut
        /// on the length, so a name near the end is left whole rather than sliced through the middle.
        /// </summary>
        public const int ExpansionDepthLimit = 4000;

        /// <summary>What a definition is cut to for a grid cell, where the whole of it is a click away.</summary>
        public const int MaxOneLineLength = 400;

        /// <summary>The most operators named as using an expression before the rest are counted.</summary>
        public const int MaxNamedUsers = 3;

        /// <summary>
        /// A name the optimiser generated: letters then digits, as in Expr1011, ConstExpr1005 and
        /// Uniq1002.  The shape rather than a list of prefixes, because the list has grown with every
        /// release and a prefix that was not on it would silently stop linking.
        /// </summary>
        private static readonly Regex GeneratedNamePattern =
            new(@"^[A-Za-z_]+[0-9]+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// A generated name used inside an expression, which showplan always writes bracketed.  The
        /// brackets are what keeps this from matching part of a function name.
        /// </summary>
        private static readonly Regex ReferencePattern =
            new(@"\[([A-Za-z_]+[0-9]+)\]", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>Anything that could be a name, for finding the uses of one in an operator's properties.</summary>
        private static readonly Regex WordPattern =
            new(@"[A-Za-z_][A-Za-z0-9_]*", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static bool IsGeneratedName(string? name) =>
            !string.IsNullOrEmpty(name) && GeneratedNamePattern.IsMatch(name!);

        /// <summary>
        /// Every generated name written inside brackets in <paramref name="text"/>, with where it is,
        /// so a caller can replace it - with the definition, or with a link to it.  Whether the
        /// statement actually defines the name is the caller's to check.
        /// </summary>
        public static IEnumerable<(int Index, int Length, string Name)> ReferencesIn(string? text)
        {
            if (string.IsNullOrEmpty(text)) yield break;

            foreach (Match match in ReferencePattern.Matches(text!))
            {
                yield return (match.Index, match.Length, match.Groups[1].Value);
            }
        }

        /// <summary>
        /// <paramref name="text"/> with every one of the plan's own names in it - [Expr1011] and the
        /// like - replaced by that name's definition, bracketed, and expanded all the way down.  An
        /// empty string when the text names none the statement defines, so a caller can tell "nothing
        /// to add here" from "the same text again".
        ///
        /// What a warning's detail says with the values it names written out: CONVERT_IMPLICIT
        /// (int,[Expr1011],0) is a wrong estimate blamed on a value the reader cannot see, and this is
        /// where they can.
        /// </summary>
        public static string ExpandedReferencesIn(PlanStatement? statement, string? text)
        {
            if (statement is null || string.IsNullOrEmpty(text)) return string.Empty;

            var expanded = new StringBuilder();
            var copied = 0;
            var replaced = false;

            foreach (var (index, length, name) in ReferencesIn(text))
            {
                if (statement.ExpressionNamed(name) is not { } expression) continue;

                expanded.Append(text, copied, index - copied).Append('(').Append(expression.Expanded).Append(')');
                copied = index + length;
                replaced = true;
            }

            return replaced ? expanded.Append(text, copied, text!.Length - copied).ToString() : string.Empty;
        }

        /// <summary>
        /// Every one of the plan's own names in <paramref name="text"/>, bracketed or bare, each
        /// once and in the order they appear.
        ///
        /// A scalar expression writes them bracketed - CONVERT_IMPLICIT(int,[Expr1011],0) - and the
        /// column lists write them bare: an order by of "Expr1005 ASC", a group by, a hash key.  Both
        /// are read as often, so anything explaining a value to the reader looks for both.  Whether
        /// the statement actually defines the name is the caller's to check, which is what keeps a
        /// column that happens to be named like one from being taken for one.
        /// </summary>
        internal static IEnumerable<string> NamesIn(string? text)
        {
            if (string.IsNullOrEmpty(text)) yield break;

            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match word in WordPattern.Matches(text!))
            {
                if (IsGeneratedName(word.Value) && found.Add(word.Value)) yield return word.Value;
            }
        }

        /// <summary>
        /// The statement's expressions, with each one's expansion and what it refers to filled in.
        /// In the order the operators define them, which is the order the plan works them out in.
        ///
        /// Where each is used is a separate pass - see <see cref="ApplyUsage"/> - because this one
        /// runs before the operators' property lists are built, so that those lists can show what a
        /// name means, and the usage pass runs after them, because it searches them.
        /// </summary>
        internal static IReadOnlyList<PlanExpression> Resolve(IReadOnlyList<PlanExpression> defined)
        {
            if (defined.Count == 0) return [];

            var byName = Index(defined);

            foreach (var expression in defined)
            {
                // Its own name counts as a reference when it resolves to a different definition -
                // which is exactly what ANY([Expr1033]) is - and does not when it resolves back here.
                expression.References = ReferencesIn(expression.Definition)
                    .Select(reference => reference.Name)
                    .Where(name => byName.TryGetValue(name, out var target) && !ReferenceEquals(target, expression))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                expression.Expanded = expression.References.Count == 0
                    ? expression.Definition
                    : Expand(expression.Definition, byName, [expression]);
            }

            return defined;
        }

        /// <summary>
        /// What each name resolves to.
        ///
        /// A name means the definition that works its value out.  A parallel aggregate defines the
        /// same name again as ANY of itself on the way up - see <see cref="PlanExpression"/> - and
        /// resolving Expr1033 to ANY([Expr1033]) would answer the question with the question.
        /// Otherwise the first definition wins, which is the one a parallel plan's branches share.
        /// </summary>
        internal static Dictionary<string, PlanExpression> Index(IEnumerable<PlanExpression> expressions)
        {
            var byName = new Dictionary<string, PlanExpression>(StringComparer.OrdinalIgnoreCase);

            foreach (var expression in expressions.Where(e => e.IsGenerated))
            {
                if (!byName.TryGetValue(expression.Name, out var found)) byName[expression.Name] = expression;
                else if (CarriesItself(found) && !CarriesItself(expression)) byName[expression.Name] = expression;
            }

            return byName;
        }

        /// <summary>
        /// <paramref name="text"/> with each name it refers to replaced by that name's definition,
        /// and so on down.  Bracketed, because a definition substituted into a larger expression is
        /// an operand of it: CONVERT(int,[Expr2]) where Expr2 is a+b means CONVERT(int,(a+b)), and
        /// leaving the brackets off would quietly change what it says.
        ///
        /// <paramref name="expanding"/> holds the definitions being written out further up - the
        /// definitions rather than the names, so that ANY([Expr1033]) still expands to the Compute
        /// Scalar's Expr1033, while anything that leads back to itself is left as a name rather than
        /// looping.
        /// </summary>
        private static string Expand(
            string text,
            IReadOnlyDictionary<string, PlanExpression> byName,
            HashSet<PlanExpression> expanding)
        {
            var expanded = new StringBuilder();
            var copied = 0;

            foreach (var (index, length, name) in ReferencesIn(text))
            {
                if (!byName.TryGetValue(name, out var referenced) || expanding.Contains(referenced)) continue;

                expanded.Append(text, copied, index - copied);
                copied = index + length;

                expanding.Add(referenced);
                expanded.Append('(').Append(Expand(referenced.Definition, byName, expanding)).Append(')');
                expanding.Remove(referenced);

                // Once the text has grown past the limit, stop substituting and leave the remaining
                // references as names.  What has already been expanded stays whole - this guards how
                // far down the expansion goes rather than trimming the finished string to a length.
                if (expanded.Length > ExpansionDepthLimit) break;
            }

            return expanded.Append(text, copied, text.Length - copied).ToString();
        }

        /// <summary>
        /// True for a definition that is only the name being passed on - ANY([Expr1033]) on the
        /// Stream Aggregate that carries Expr1033 up out of a group.  It says where the value went,
        /// not what it is.
        /// </summary>
        private static bool CarriesItself(PlanExpression expression) =>
            ReferencesIn(expression.Definition)
                .Any(reference => string.Equals(reference.Name, expression.Name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Record against each expression the operators that mention its name.
        ///
        /// Every operator's whole property list is searched rather than its predicates alone - see
        /// <see cref="PlanOperatorText"/> - because a name turns up in an output list, a sort key, a
        /// hash key, a group by and a probe column as well, and those are exactly the places where
        /// knowing what it means is the point.
        /// </summary>
        internal static void ApplyUsage(IReadOnlyList<PlanExpression> expressions, IEnumerable<PlanOperator> operators)
        {
            if (expressions.Count == 0) return;

            var byName = Index(expressions);
            if (byName.Count == 0) return;

            var users = new Dictionary<string, List<PlanOperator>>(StringComparer.OrdinalIgnoreCase);

            foreach (var op in operators)
            {
                foreach (var name in NamesIn(op, byName))
                {
                    // The operator that defines it is shown as the definition, not as a use of it.
                    if (byName.TryGetValue(name, out var defined) && ReferenceEquals(defined.DefinedBy, op)) continue;

                    if (!users.TryGetValue(name, out var list)) users[name] = list = [];
                    list.Add(op);
                }
            }

            foreach (var pair in byName)
            {
                if (users.TryGetValue(pair.Key, out var list)) pair.Value.UsedBy = list;
            }
        }

        /// <summary>The generated names one operator mentions anywhere, each once.</summary>
        private static IEnumerable<string> NamesIn(PlanOperator op, IReadOnlyDictionary<string, PlanExpression> byName)
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var text in PlanOperatorText.Of(op))
            {
                foreach (Match word in WordPattern.Matches(text))
                {
                    if (byName.ContainsKey(word.Value)) found.Add(word.Value);
                }
            }

            return found;
        }
    }
}
