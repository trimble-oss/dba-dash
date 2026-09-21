using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// The warnings showplan carries, as a closed list.
    ///
    /// SQL Server reports these in several shapes - some are attributes on the Warnings element, some
    /// are child elements with their own detail - and the viewer treats them uniformly: a badge on
    /// the operator and a row in the warnings list.  Anything new or unrecognised lands on
    /// <see cref="Other"/> and is still shown, with whatever text the plan gave.
    /// </summary>
    public enum PlanWarningKind
    {
        Other,

        /// <summary>A join with no predicate, which is a cross join whether or not that was intended.</summary>
        NoJoinPredicate,

        /// <summary>An operator ran out of its memory grant and went to tempdb.</summary>
        SpillToTempDb,

        /// <summary>A sort spilled, with the spill level and pass count.</summary>
        SortSpill,

        /// <summary>A hash operator spilled and had to recurse or bail out.</summary>
        HashSpill,

        /// <summary>An exchange operator spilled, which is usually a symptom rather than the cause.</summary>
        ExchangeSpill,

        /// <summary>The optimiser had to guess because a column had no statistics.</summary>
        ColumnsWithNoStatistics,

        /// <summary>Statistics existed but were out of date.</summary>
        ColumnsWithStaleStatistics,

        /// <summary>
        /// A conversion the optimiser could not see through - the implicit conversion that stops a
        /// seek, or one that wrecks the estimate.
        /// </summary>
        PlanAffectingConvert,

        /// <summary>The grant was far larger or smaller than what the query actually used.</summary>
        MemoryGrantWarning,

        /// <summary>An index could not be used because of a filter or a type mismatch.</summary>
        UnmatchedIndexes,

        /// <summary>A spatial index guess.</summary>
        SpatialGuess,

        /// <summary>An online index build forced a full update.</summary>
        FullUpdateForOnlineIndexBuild,

        /// <summary>A wait recorded against the operator.</summary>
        Wait
    }

    /// <summary>How much attention a warning deserves.</summary>
    public enum PlanWarningSeverity
    {
        /// <summary>Worth knowing, not necessarily wrong.</summary>
        Information,

        /// <summary>Likely to be costing something.</summary>
        Warning,

        /// <summary>Almost always the answer to "why is this slow".</summary>
        Critical
    }

    /// <summary>
    /// One warning, either on an operator or on the plan as a whole.
    ///
    /// <see cref="Title"/> is what goes on a badge and in a list; <see cref="Detail"/> carries the
    /// specifics - which columns, which conversion, how much memory - and is null when the plan gave
    /// none.
    /// </summary>
    public sealed class PlanWarning
    {
        internal PlanWarning(
            PlanWarningKind kind,
            string title,
            string? detail = null,
            PlanWarningSeverity severity = PlanWarningSeverity.Warning)
        {
            Kind = kind;
            Title = title;
            Detail = detail;
            Severity = severity;
        }

        public PlanWarningKind Kind { get; }

        public string Title { get; }

        public string? Detail { get; }

        public PlanWarningSeverity Severity { get; }

        /// <summary>
        /// For a warning showplan reported against the statement rather than against an operator -
        /// a plan affecting conversion - the operators it was traced to: see
        /// <see cref="PlanWarningLocator"/> for how, and why it is worth the trouble.
        ///
        /// Empty for a warning that is already on an operator, where the operator carrying it is the
        /// answer, and for one nothing in the plan could be matched to.
        /// </summary>
        public IReadOnlyList<PlanOperator> Operators { get; internal set; } = [];

        /// <summary>Where it was traced to, for a list that has to say so in one column.</summary>
        public string OperatorsDescription =>
            PlanFormat.List(
                Operators.Select(op => op.DisplayName + " (node " + op.NodeId.ToString(CultureInfo.InvariantCulture) + ")").ToList(),
                MaxNamedOperators);

        /// <summary>The most operators named as carrying one warning before the rest are counted.</summary>
        private const int MaxNamedOperators = 3;

        /// <summary>
        /// True for the warnings that mean the query gave up on memory and used tempdb instead,
        /// whichever operator reported it.  Grouped because the response is the same.
        /// </summary>
        public bool IsSpill =>
            Kind is PlanWarningKind.SpillToTempDb
                or PlanWarningKind.SortSpill
                or PlanWarningKind.HashSpill
                or PlanWarningKind.ExchangeSpill;

        public override string ToString() => Detail is null ? Title : Title + ": " + Detail;
    }

    /// <summary>
    /// An index the optimiser says it wanted.
    ///
    /// Reported at the plan level, but always about one table, so the viewer also points it at the
    /// operator that reads that table - a missing index list that does not say where to look is a
    /// lot less useful than one that does.
    /// </summary>
    public sealed class PlanMissingIndex
    {
        /// <summary>
        /// The optimiser's own estimate of how much cheaper the query would be, as a percentage.
        /// Worth reading as a ranking rather than a promise.
        /// </summary>
        public double Impact { get; internal set; }

        public string? Database { get; internal set; }

        public string? Schema { get; internal set; }

        public string? Table { get; internal set; }

        /// <summary>Columns used for equality predicates - the leading key columns.</summary>
        public IReadOnlyList<string> EqualityColumns { get; internal set; } = [];

        /// <summary>Columns used for range predicates - key columns after the equality ones.</summary>
        public IReadOnlyList<string> InequalityColumns { get; internal set; } = [];

        /// <summary>Columns needed only to avoid a lookup.</summary>
        public IReadOnlyList<string> IncludedColumns { get; internal set; } = [];

        /// <summary>True when the recommendation is about a temp table rather than a permanent one.</summary>
        public bool IsTempTable => Table is not null && Table.StartsWith('#');

        /// <summary>The fully qualified table, for matching against the operator that reads it.</summary>
        public string QualifiedTableName
        {
            get
            {
                var parts = new List<string>(3);
                if (!string.IsNullOrEmpty(Database)) parts.Add(Database);
                if (!string.IsNullOrEmpty(Schema)) parts.Add(Schema);
                if (!string.IsNullOrEmpty(Table)) parts.Add(Table);
                return string.Join('.', parts);
            }
        }

        /// <summary>
        /// A runnable CREATE INDEX statement, with a generated name.
        ///
        /// Written out in full rather than left to the reader because this is the one thing anybody
        /// does with a missing index recommendation, and transcribing three column lists by hand is
        /// where mistakes come from.  The name is descriptive rather than clever - it has to be
        /// changed to suit local conventions anyway, and a name that says what the index is for is
        /// easier to edit than one that says nothing.
        /// </summary>
        public string CreateStatement
        {
            get
            {
                var keys = KeyColumns();
                var name = IndexName();

                var statement = "CREATE NONCLUSTERED INDEX [" + name + "]\n    ON " + Bracketed() +
                                " (" + string.Join(", ", Quote(keys)) + ")";

                if (IncludedColumns.Count > 0)
                {
                    statement += "\n    INCLUDE (" + string.Join(", ", Quote(IncludedColumns)) + ")";
                }

                return statement + ";";
            }
        }

        /// <summary>
        /// The same index written the way it would go inside a CREATE TABLE, which is how an index on
        /// a temp table is best declared.
        ///
        /// Adding an index to a temp table after the table exists is DDL on that table, and DDL stops
        /// SQL Server reusing a cached temp table definition between executions of the procedure that creates it -
        /// so an index meant to make the statement faster hands back the allocation and the tempdb
        /// metadata work that the caching was saving.  Declared on the CREATE TABLE the caching is
        /// kept.  Needs SQL Server 2014, or 2016 when there are included columns.
        /// </summary>
        public string InlineIndexDefinition
        {
            get
            {
                var definition = "INDEX [" + IndexName() + "] NONCLUSTERED (" +
                                 string.Join(", ", Quote(KeyColumns())) + ")";

                return IncludedColumns.Count > 0
                    ? definition + " INCLUDE (" + string.Join(", ", Quote(IncludedColumns)) + ")"
                    : definition;
            }
        }

        /// <summary>The key, equality columns first, then the inequality ones.</summary>
        private List<string> KeyColumns()
        {
            var keys = new List<string>(EqualityColumns.Count + InequalityColumns.Count);
            keys.AddRange(EqualityColumns);
            keys.AddRange(InequalityColumns);
            return keys;
        }

        private string IndexName()
        {
            var name = "IX_" + (Table ?? "Table");
            foreach (var column in KeyColumns()) name += "_" + column;

            // A name longer than sysname will not create, and a truncated tail is no worse than the
            // rename this needs anyway.
            return name.Length > 116 ? name[..116] : name;
        }

        /// <summary>
        /// <see cref="CreateStatement"/> on one line, for a grid cell: a grid shows one line a row,
        /// and a copied cell that runs as it is beats one that has to be reassembled.
        /// </summary>
        public string CreateStatementOneLine =>
            string.Join(" ", CreateStatement.Split('\n').Select(line => line.Trim()));

        private string Bracketed()
        {
            // A temp table is qualified tempdb.dbo on the plan, but the index has to be created from
            // the session that owns the table and by the name that session knows it by, so the
            // qualifier would only stop the script running where it has to run.
            if (IsTempTable) return "[" + Table + "]";

            var parts = new List<string>(3);
            if (!string.IsNullOrEmpty(Database)) parts.Add("[" + Database + "]");
            if (!string.IsNullOrEmpty(Schema)) parts.Add("[" + Schema + "]");
            if (!string.IsNullOrEmpty(Table)) parts.Add("[" + Table + "]");
            return string.Join('.', parts);
        }

        private static IEnumerable<string> Quote(IEnumerable<string> columns)
        {
            foreach (var column in columns) yield return "[" + column + "]";
        }
    }

    /// <summary>
    /// A parameter the plan was compiled with, and the value it ran with.
    ///
    /// The pair is the whole point: a plan compiled for one value and run with a very different one
    /// is parameter sniffing, and it is invisible unless both values are put side by side.
    /// </summary>
    public sealed class PlanParameter
    {
        public string Name { get; internal set; } = string.Empty;

        /// <summary>The declared type, where the plan says - SQL Server 2017 and later.</summary>
        public string? DataType { get; internal set; }

        /// <summary>The value the plan was compiled for, as showplan wrote it.</summary>
        public string? CompiledValue { get; internal set; }

        /// <summary>The value this execution supplied.  Null on an estimated plan.</summary>
        public string? RuntimeValue { get; internal set; }

        /// <summary>
        /// The plan was compiled for a different value than it ran with.  Not a problem in itself -
        /// most reuse is fine - but it is the first thing to check when one execution is slow and
        /// the next is not.
        /// </summary>
        public bool CompiledValueDiffers =>
            RuntimeValue is not null && CompiledValue is not null && RuntimeValue != CompiledValue;
    }

    /// <summary>
    /// Works out which operator a plan level warning is actually about.
    ///
    /// Showplan reports a plan affecting conversion against the statement, not against an operator,
    /// so the picture marks the SELECT at the end of the plan and nothing else - which says a
    /// conversion is costing the query an estimate, and leaves the reader to find where it happens
    /// by opening operators one at a time.  The warning carries the expression, and the operator
    /// that evaluates that expression carries the same text in its predicate or its defined values,
    /// so the two can simply be matched up.
    ///
    /// Only conversions: they are the one warning whose detail is an expression.  Everything else
    /// showplan puts at the statement level is about the statement - a memory grant, an optimiser
    /// time out - and has no one operator to point at.
    /// </summary>
    internal static class PlanWarningLocator
    {
        public static void Locate(PlanStatement statement)
        {
            var convertWarnings = statement.Warnings
                .Where(w => w.Kind == PlanWarningKind.PlanAffectingConvert && !string.IsNullOrEmpty(w.Detail))
                .ToList();

            if (convertWarnings.Count == 0) return;

            // Each operator's text joined once, because every warning is looked for in every operator.
            var operators = statement.Operators
                .Select(op => (Operator: op, Text: string.Join("\n", PlanOperatorText.Of(op))))
                .ToList();

            foreach (var warning in convertWarnings)
            {
                warning.Operators = operators
                    .Where(candidate => Mentions(candidate.Text, warning.Detail!))
                    .Select(candidate => candidate.Operator)
                    .ToList();
            }
        }

        /// <summary>
        /// True when an operator's text holds the warning's expression.
        ///
        /// The whole expression first, then - where the warning wrote the comparison the conversion
        /// took part in and the operator wrote a different one, which happens when one predicate
        /// becomes a range - the conversion on its own.
        /// </summary>
        private static bool Mentions(string text, string expression)
        {
            if (text.Contains(expression, StringComparison.OrdinalIgnoreCase)) return true;

            var call = LeadingCall(expression);
            return call is not null && text.Contains(call, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The function call an expression starts with - the CONVERT_IMPLICIT(...) of
        /// CONVERT_IMPLICIT(...)&gt;(10) - or null when it does not start with one, or is nothing but
        /// one.  Brackets are counted rather than searched for, since the arguments contain their own.
        /// </summary>
        private static string? LeadingCall(string expression)
        {
            var open = expression.IndexOf('(');
            if (open <= 0) return null;

            // Everything before the bracket has to be a name for this to be a call at all.
            for (var i = 0; i < open; i++)
            {
                if (!char.IsLetterOrDigit(expression[i]) && expression[i] != '_') return null;
            }

            var depth = 0;
            for (var i = open; i < expression.Length; i++)
            {
                if (expression[i] == '(') depth++;
                else if (expression[i] == ')' && --depth == 0)
                {
                    var call = expression[..(i + 1)];
                    return call.Length == expression.Length ? null : call;
                }
            }

            return null;
        }
    }

    /// <summary>A wait recorded for the query, from the WaitStats element of an actual plan.</summary>
    public sealed class PlanWaitStat
    {
        public string WaitType { get; internal set; } = string.Empty;

        public long WaitTimeMs { get; internal set; }

        public long WaitCount { get; internal set; }

        /// <summary>The average length of one wait, or null when none were counted.</summary>
        public double? AverageWaitMs => WaitCount > 0 ? WaitTimeMs / (double)WaitCount : null;
    }
}
