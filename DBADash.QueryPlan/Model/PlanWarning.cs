using System.Collections.Generic;
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
