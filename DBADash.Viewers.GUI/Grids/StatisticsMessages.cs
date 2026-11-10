using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DBADashGUI.Grids
{
    /// <summary>One table's I/O from a SET STATISTICS IO line, its counters in the order SQL Server printed them.</summary>
    public sealed class StatisticsTableIO
    {
        public StatisticsTableIO(string table) => Table = table;

        public string Table { get; }

        /// <summary>Counter name, as <see cref="StatisticsMessages.CounterName"/> spells it, to value.</summary>
        public Dictionary<string, long> Counters { get; } = new(StringComparer.OrdinalIgnoreCase);

        public long Counter(string name) => Counters.GetValueOrDefault(name);
    }

    /// <summary>
    /// What the messages say about one statement: the compile time printed before it, its row count, the I/O of each
    /// table it read, and its execution time.  Where one statement ends and the next begins isn't in the messages, so
    /// it's inferred - see <see cref="StatisticsMessages.Parse"/>.
    /// </summary>
    public sealed class StatisticsStatement
    {
        public int Number { get; internal set; }
        public long? RowsAffected { get; internal set; }
        public long? CompileCpuMs { get; internal set; }
        public long? CompileElapsedMs { get; internal set; }
        public long? CpuMs { get; internal set; }
        public long? ElapsedMs { get; internal set; }
        public List<StatisticsTableIO> Tables { get; } = new();

        public long Counter(string name) => Tables.Sum(t => t.Counter(name));

        /// <summary>
        /// The query's own time, where it's a single statement: it includes everything printed before it that the
        /// statement ran - a procedure's statements, nested procedures, dynamic SQL, triggers - so it's the total, not
        /// another statement to add.
        /// </summary>
        public bool IsTotal { get; internal set; }

        internal bool IsEmpty => RowsAffected == null && CompileCpuMs == null && CpuMs == null && Tables.Count == 0;

        internal bool HasOutput => RowsAffected != null || CpuMs != null || Tables.Count > 0;

        /// <summary>
        /// Did nothing measurable: no I/O and no time - a SET statement, say.  A row count alone doesn't count: an
        /// INSERT's is printed after its execution time, on a line of its own.
        /// </summary>
        internal bool IsTrivial => Tables.Count == 0 && (CpuMs ?? 0) == 0 && (ElapsedMs ?? 0) == 0;
    }

    /// <summary>What a query's text says about how its times add up - see <see cref="StatisticsMessages.ReadQuery"/>.</summary>
    /// <param name="Statements">Statements that do something - not SET, USE, PRINT or a DECLARE without a value.</param>
    /// <param name="Calls">EXECs anywhere in it: procedures, sp_executesql, dynamic SQL, INSERT ... EXEC.</param>
    /// <param name="Modifications">INSERT, UPDATE, DELETE and MERGE statements anywhere in it - any could fire a trigger.</param>
    /// <param name="SingleStatement">The one statement, on one line, when there's only one and it prints its own time.</param>
    public sealed record QueryShape(int Statements, int Calls, int Modifications, string SingleStatement)
    {
        /// <summary>The single statement runs other code, all of whose time - its compiles included - is in its own.</summary>
        public bool SingleIsCall => SingleStatement != null && Calls > 0;
    }

    /// <summary>
    /// One run of a comparison: a part of a query split at PRINT 'DBADash-Compare', or another tab's query - named for
    /// its marker's label, its single statement, or where it came from.
    /// </summary>
    public sealed record StatisticsRun(string Label, StatisticsReport Report);

    /// <summary>
    /// A query's text parsed once, for everything read from it - see <see cref="StatisticsMessages.ParseQuery"/>.
    /// </summary>
    /// <param name="Text">The text, which the statements' offsets are into.</param>
    /// <param name="Statements">Its top-level statements, across all of its batches.</param>
    /// <param name="MayHaveChanged">Read from the editor when the messages were opened, rather than as it ran.</param>
    public sealed record ParsedQuery(string Text, IReadOnlyList<TSqlStatement> Statements, bool MayHaveChanged);

    /// <summary>The statements found in a query's messages, and the totals across them.</summary>
    public sealed class StatisticsReport
    {
        public List<StatisticsStatement> Statements { get; } = new();

        /// <summary>Every I/O counter seen, in the order first seen - SQL Server's own order on its line.</summary>
        public List<string> CounterNames { get; } = new();

        /// <summary>SSMS's "Completion time" line, as it printed it.</summary>
        public string CompletionTime { get; internal set; }

        /// <summary>How many "Msg ..., Level ..." errors there were.</summary>
        public int ErrorCount { get; internal set; }

        /// <summary>True if SET STATISTICS IO or TIME printed anything - row counts alone aren't statistics.</summary>
        public bool HasStatistics => HasIO || HasTime;

        public bool HasIO => Statements.Any(s => s.Tables.Count > 0);

        public bool HasTime => Statements.Any(s => s.CpuMs != null || s.CompileCpuMs != null);

        /// <summary>What the query's text says about it, where SSMS sent the text and it parsed; null otherwise.</summary>
        public QueryShape Query { get; private set; }

        /// <summary>
        /// True where the query text was read from the editor when the messages were opened, rather than as the query
        /// ran - so it may not be the query that printed them.
        /// </summary>
        public bool QueryMayHaveChanged { get; private set; }

        /// <summary>
        /// True where the text is a single statement but the messages don't fit one - other statements did something
        /// alongside a plain SELECT, or after its time - so they're from some other query, and are added up instead.
        /// </summary>
        public bool QueryDoesNotFit => Query?.SingleStatement != null && Total == null && Statements.Any(s => s.CpuMs != null);

        /// <summary>
        /// The query's one statement, as written - "EXEC dbo.sp_WhoIsActive" - when it is one, and its own time was found:
        /// <see cref="Total"/>.  Null otherwise, and the times are the sum of every statement's.
        /// </summary>
        public string SingleStatement => Total != null ? Query?.SingleStatement : null;

        /// <summary>True when <see cref="SingleStatement"/> is a call: a procedure, sp_executesql or dynamic SQL.</summary>
        public bool SingleIsCall => Total != null && Query?.SingleIsCall == true;

        /// <summary>The single statement's own time, which includes everything it ran - see <see cref="SingleStatement"/>.</summary>
        public StatisticsStatement Total => Statements.FirstOrDefault(s => s.IsTotal);

        /// <summary>True when <see cref="Total"/> is printed alongside others that did something - what it ran.</summary>
        public bool TotalIncludesOthers => Total != null && Statements.Any(s => !s.IsTotal && !s.IsTrivial);

        // With a single statement: the index of the first statement that did something measurable.  Those before it did
        // nothing worth counting - the SET statements before the call, say.
        private int _firstInside;

        /// <summary>Compile printed before anything else - a plain statement's own compile, which precedes it running.</summary>
        internal long CompileCpuMsBeforeOutput { get; set; }
        internal long CompileElapsedMsBeforeOutput { get; set; }

        /// <summary>
        /// True for a statement whose time is counted in the totals: every one, unless the query is a single statement -
        /// then only its own time and what ran outside it, as everything it ran is already in its time.
        /// </summary>
        private static bool CountsTime(StatisticsStatement statement, StatisticsStatement total, int firstInside) =>
            total == null || statement.IsTotal || statement.Number - 1 < firstInside || statement.Number > total.Number;

        /// <summary>The statements whose time is counted - see <see cref="CountsTime"/> - finding the total only once.</summary>
        private IEnumerable<StatisticsStatement> TimedStatements()
        {
            var total = Total;
            return Statements.Where(s => CountsTime(s, total, _firstInside));
        }

        public long CpuMs => TimedStatements().Sum(s => s.CpuMs ?? 0);
        public long ElapsedMs => TimedStatements().Sum(s => s.ElapsedMs ?? 0);

        private long AllCompileCpuMs => Statements.Sum(s => s.CompileCpuMs ?? 0);
        private long AllCompileElapsedMs => Statements.Sum(s => s.CompileElapsedMs ?? 0);

        /// <summary>
        /// Compile time on top of the execution time.  All of it, as SSMS prints it - unless the query is a single
        /// statement.  A call (a procedure, sp_executesql or dynamic SQL) compiles everything inside its own time, its own
        /// compile included; a plain statement compiles before it runs, so only that is on top, and what it compiles as it
        /// runs - a trigger - is in its time.
        /// </summary>
        public long CompileCpuMs => Total == null ? AllCompileCpuMs : SingleIsCall ? 0 : CompileCpuMsBeforeOutput;

        public long CompileElapsedMs => Total == null ? AllCompileElapsedMs : SingleIsCall ? 0 : CompileElapsedMsBeforeOutput;

        /// <summary>Compile while the single statement ran - already in its time, so not in <see cref="CompileCpuMs"/>.</summary>
        public long CompileCpuMsInside => AllCompileCpuMs - CompileCpuMs;

        public long CompileElapsedMsInside => AllCompileElapsedMs - CompileElapsedMs;

        /// <summary>Every statement's execution CPU added up - what the totals would be without <see cref="SingleStatement"/>.</summary>
        public long SumOfStatementsCpuMs => Statements.Sum(s => s.CpuMs ?? 0);

        /// <summary>
        /// The longest single execution CPU printed - the least the query can have taken, as every time printed is part of
        /// it.  With several calls, the messages don't say which statements ran inside which, so that and everything
        /// added up are the honest bounds.
        /// </summary>
        public long LargestCpuMs => Statements.Select(s => s.CpuMs ?? 0).DefaultIfEmpty().Max();

        /// <summary>The longest single execution elapsed time printed - see <see cref="LargestCpuMs"/>.</summary>
        public long LargestElapsedMs => Statements.Select(s => s.ElapsedMs ?? 0).DefaultIfEmpty().Max();

        /// <summary>
        /// I/O is only ever printed for the statements that did it - a procedure prints no total of its own - so it's
        /// added up either way.
        /// </summary>
        public long Counter(string name) => Statements.Sum(s => s.Counter(name));

        /// <summary>
        /// Notes what the query's text says, and where it's a single statement, takes its own time as the total: the
        /// execution block that encloses all the others - so the longest, and the last on a tie - provided nothing
        /// measurable ran after it.  Otherwise the messages don't fit a single statement (the query text has changed since
        /// it ran, say), and the statements are added up as usual.
        /// </summary>
        internal void ApplyQuery(QueryShape query, bool mayHaveChanged = false)
        {
            Query = query;
            QueryMayHaveChanged = query != null && mayHaveChanged;
            if (query?.SingleStatement == null) return;

            var total = Statements.Where(s => s.CpuMs != null)
                .OrderByDescending(s => s.ElapsedMs)
                .ThenByDescending(s => s.CpuMs)
                .ThenByDescending(s => s.Number)
                .FirstOrDefault();
            if (total == null) return;

            var index = total.Number - 1;
            if (Statements.Skip(index + 1).Any(s => !s.IsTrivial)) return;

            // A plain SELECT runs nothing that prints a time of its own - no procedure, no trigger - so anything else
            // measurable means the messages are from some other query: the text was changed, or another part selected.
            if (query.Calls == 0 && query.Modifications == 0 && Statements.Take(index).Any(s => !s.IsTrivial)) return;

            var firstInside = Statements.Take(index).ToList().FindIndex(s => !s.IsTrivial);
            _firstInside = firstInside < 0 ? index : firstInside;
            total.IsTotal = true;
        }
    }

    /// <summary>
    /// Reads the output of SET STATISTICS IO and SET STATISTICS TIME from a query's messages - as SSMS shows them on
    /// its Messages tab - into tables: I/O by table, I/O by statement, and time and reads by statement.
    ///
    /// Only SQL Server's English messages are recognised.  Anything else - PRINT output, errors, warnings - is passed
    /// over, so a line not understood never stops the rest being read.  The counters on a Table line are read by name
    /// rather than position, as the set has grown over versions (page server reads came with Hyperscale, segment reads
    /// are a line of their own for columnstore), so a version that adds another just gets another column.
    /// </summary>
    public static class StatisticsMessages
    {
        public const string ScanCount = "Scan count";
        public const string LogicalReads = "Logical reads";
        public const string PhysicalReads = "Physical reads";
        public const string ReadAheadReads = "Read-ahead reads";

        /// <summary>Shown even when they're all zero; any other counter only when something has a value for it.</summary>
        private static readonly string[] CoreCounters = [ScanCount, LogicalReads, PhysicalReads, ReadAheadReads];

        // Table 'name'. Scan count 1, logical reads 2, ...  The name can hold anything, a quote or a dot included, so
        // it's everything up to the last "'. " that is followed by counters.
        private static readonly Regex TableLine = new(
            @"^Table '(?<table>.*)'\.\s+(?<counters>[A-Za-z][A-Za-z \-]*\s+-?\d+(?:\s*,\s*[A-Za-z][A-Za-z \-]*\s+-?\d+)*)\s*\.?\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex Counter = new(@"^(?<name>[A-Za-z][A-Za-z \-]*?)\s+(?<value>-?\d+)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex CpuLine = new(
            @"CPU time = (?<cpu>\d+) ms,\s*elapsed time = (?<elapsed>\d+) ms",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        // (1 row affected), (5 rows affected), and the older (1 row(s) affected).
        private static readonly Regex RowsAffected = new(@"^\((?<rows>\d+) rows?(?:\(s\))? affected\)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex Error = new(@"^Msg \d+, Level \d+, State \d+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private const string CompileHeader = "SQL Server parse and compile time:";
        private const string ExecutionHeader = "SQL Server Execution Times:";
        private const string CompletionTimePrefix = "Completion time:";

        private enum Pending
        {
            None,
            Compile,
            Execution
        }

        public static StatisticsReport Parse(string messages) =>
            Parse((messages ?? string.Empty).Split('\n'));

        /// <summary>
        /// As <see cref="Parse(string)"/>, reading <paramref name="queryText"/> - the query that printed the messages -
        /// too: where it's a single statement, its own time is taken as the total rather than added to what it ran, and
        /// the card can say how far the totals can be trusted otherwise.
        /// </summary>
        public static StatisticsReport Parse(string messages, string queryText) => Parse(messages, ParseQuery(queryText));

        /// <summary>As <see cref="Parse(string, string)"/>, with the query already parsed - see <see cref="ParseQuery"/>.</summary>
        public static StatisticsReport Parse(string messages, ParsedQuery query)
        {
            var report = Parse(messages);
            report.ApplyQuery(query == null ? null : ShapeOf(query.Text, query.Statements), query?.MayHaveChanged == true);
            return report;
        }

        /// <summary>
        /// <paramref name="queryText"/> parsed, once, for everything read from it - its shape, and its parts to compare.
        /// <paramref name="mayHaveChanged"/> where it was read from the editor when the messages were opened, rather than
        /// as it ran.  Null for no text, or text that doesn't parse.
        /// </summary>
        public static ParsedQuery ParseQuery(string queryText, bool mayHaveChanged = false)
        {
            if (string.IsNullOrWhiteSpace(queryText)) return null;

            var parser = new TSql170Parser(initialQuotedIdentifiers: true);
            using var reader = new System.IO.StringReader(queryText);
            if (parser.Parse(reader, out var errors) is not TSqlScript script || errors.Count > 0) return null;

            return new ParsedQuery(queryText, script.Batches.SelectMany(b => b.Statements).ToList(), mayHaveChanged);
        }

        /// <summary>
        /// What <paramref name="queryText"/> says about how its times add up: how many statements do something (not SET,
        /// USE, PRINT or a DECLARE without a value), how many EXECs and data changes are in it anywhere, and - where
        /// there's only one statement, across any number of batches, and it prints its own time - that statement on one
        /// line.  A procedure called without EXEC, as the first statement of a batch, is an EXEC too.  Control of flow
        /// (IF, WHILE, a BEGIN ... END block) prints no time of its own, so a query that's one of those isn't a single
        /// statement.  Null for no text, or text that doesn't parse.
        /// </summary>
        public static QueryShape ReadQuery(string queryText) =>
            ParseQuery(queryText) is { } query ? ShapeOf(query.Text, query.Statements) : null;

        /// <summary><see cref="ReadQuery"/> for some of a parsed query's top-level statements.</summary>
        private static QueryShape ShapeOf(string queryText, IReadOnlyList<TSqlStatement> topLevel)
        {
            var counter = new StatementCounter();
            foreach (var statement in topLevel) statement.Accept(counter);

            var statements = topLevel.Where(s => !IsSetup(s)).ToList();
            string single = null;
            if (statements is [var only] && PrintsOwnTime(only))
            {
                single = Regex.Replace(queryText.Substring(only.StartOffset, only.FragmentLength), @"\s+", " ").Trim();
                if (single.Length > 120) single = single[..117] + "...";
            }

            return new QueryShape(statements.Count, counter.Calls, counter.Modifications, single);
        }

        /// <summary>
        /// PRINTed between the parts of a query to compare them: PRINT 'DBADash-Compare', or PRINT 'DBADash-Compare: label'
        /// to name the part that follows.
        /// </summary>
        public const string CompareMarker = "DBADash-Compare";

        private static readonly Regex CompareLine = new($@"^{Regex.Escape(CompareMarker)}\s*(?::\s*(?<label>.*?))?\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private const int MaxLabelLength = 60;

        /// <summary>
        /// Splits a query's messages into the runs to compare, at each line <see cref="CompareMarker"/> printed - read
        /// as if each were a query of its own, with its part of <paramref name="queryText"/>, so a run that's a single
        /// call has its own time taken as the total.  A part with nothing in it - before the first marker, say - isn't a
        /// run.  Null unless there are at least two runs with statistics.
        /// </summary>
        public static IReadOnlyList<StatisticsRun> SplitRuns(string messages, string queryText) =>
            SplitRuns(messages, ParseQuery(queryText));

        /// <summary>As <see cref="SplitRuns(string, string)"/>, with the query already parsed - see <see cref="ParseQuery"/>.</summary>
        public static IReadOnlyList<StatisticsRun> SplitRuns(string messages, ParsedQuery query)
        {
            var parts = SplitMessages(messages);
            if (parts.Count < 2) return null;

            // Only trusted where it splits into as many parts: a marker inside an IF or a procedure doesn't split the text.
            var queries = query == null ? null : SplitStatements(query.Statements);
            if (queries?.Count != parts.Count) queries = null;

            var runs = new List<StatisticsRun>();
            for (var i = 0; i < parts.Count; i++)
            {
                var report = Parse(parts[i].Lines);
                var shape = queries == null ? null : ShapeOf(query.Text, queries[i]);
                if (shape != null ? shape.Statements == 0 : report.Statements.All(s => s.IsTrivial)) continue;
                report.ApplyQuery(shape, query?.MayHaveChanged == true);
                if (!report.HasStatistics) continue;

                var label = parts[i].Label ?? report.SingleStatement ?? shape?.SingleStatement ?? $"Run {runs.Count + 1}";
                runs.Add(new StatisticsRun(Shorten(label), report));
            }

            return runs.Count >= 2 ? UniqueLabels(runs) : null;
        }

        private static string Shorten(string label) =>
            label.Length > MaxLabelLength ? label[..(MaxLabelLength - 3)] + "..." : label;

        /// <summary>The runs, with " (2)", " (3)" after any label already used - so each names its own column.</summary>
        public static IReadOnlyList<StatisticsRun> UniqueLabels(IEnumerable<StatisticsRun> runs)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var unique = new List<StatisticsRun>();
            foreach (var run in runs)
            {
                var label = run.Label;
                for (var n = 2; !used.Add(label); n++) label = $"{run.Label} ({n})";
                unique.Add(run with { Label = label });
            }
            return unique;
        }

        /// <summary>
        /// The messages' lines, split at each marker line - with the label it gives the part after it.  The marker's own
        /// PRINT prints an execution time straight after it, which is no part of either run, so that's left out.
        /// </summary>
        private static List<(string Label, List<string> Lines)> SplitMessages(string messages)
        {
            var parts = new List<(string Label, List<string> Lines)> { (null, new List<string>()) };
            var afterMarker = 0; // 1: the PRINT's execution time may follow; 2: its header was seen, its CPU line next.
            foreach (var raw in (messages ?? string.Empty).Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                var trimmed = line.Trim();
                var marker = CompareLine.Match(trimmed);
                if (marker.Success)
                {
                    var label = marker.Groups["label"].Value;
                    parts.Add((label.Length > 0 ? label : null, new List<string>()));
                    afterMarker = 1;
                    continue;
                }

                if (afterMarker > 0)
                {
                    if (trimmed.Length == 0) continue;
                    if (afterMarker == 1 && trimmed.Equals(ExecutionHeader, StringComparison.OrdinalIgnoreCase))
                    {
                        afterMarker = 2;
                        continue;
                    }
                    var printTime = afterMarker == 2 && CpuLine.IsMatch(trimmed);
                    afterMarker = 0;
                    if (printTime) continue;
                }

                parts[^1].Lines.Add(line);
            }
            return parts;
        }

        /// <summary>
        /// The query's text between its top-level PRINT <see cref="CompareMarker"/> statements - an empty part where
        /// there's nothing between two, or before the first.  Null for no text, text that doesn't parse, or no marker.
        /// </summary>
        public static List<string> SplitQuery(string queryText) =>
            ParseQuery(queryText) is { } query && SplitStatements(query.Statements) is { } parts
                ? parts.Select(p => p.Count == 0
                    ? string.Empty
                    : query.Text[p[0].StartOffset..(p[^1].StartOffset + p[^1].FragmentLength)]).ToList()
                : null;

        /// <summary>The top-level statements between PRINT <see cref="CompareMarker"/>s, or null with no marker.</summary>
        private static List<List<TSqlStatement>> SplitStatements(IReadOnlyList<TSqlStatement> statements)
        {
            var parts = new List<List<TSqlStatement>> { new() };
            foreach (var statement in statements)
            {
                if (statement is PrintStatement { Expression: StringLiteral text } && CompareLine.IsMatch(text.Value.Trim()))
                {
                    parts.Add(new List<TSqlStatement>());
                    continue;
                }
                parts[^1].Add(statement);
            }

            return parts.Count > 1 ? parts : null;
        }

        /// <summary>A statement that prints an execution time of its own, which includes everything it runs.</summary>
        private static bool PrintsOwnTime(TSqlStatement statement) =>
            statement is ExecuteStatement or SelectStatement or InsertStatement or UpdateStatement or DeleteStatement
                or MergeStatement;

        /// <summary>Counts the EXECs and data changes anywhere in a script - inside IF, TRY and the rest too.</summary>
        private sealed class StatementCounter : TSqlFragmentVisitor
        {
            public int Calls { get; private set; }
            public int Modifications { get; private set; }

            // ExecuteSpecification rather than ExecuteStatement, so INSERT ... EXEC is a call too.
            public override void Visit(ExecuteSpecification node) => Calls++;
            public override void Visit(InsertStatement node) => Modifications++;
            public override void Visit(UpdateStatement node) => Modifications++;
            public override void Visit(DeleteStatement node) => Modifications++;
            public override void Visit(MergeStatement node) => Modifications++;
        }

        /// <summary>A statement that only sets up the session - takes no time worth counting, and calls nothing.</summary>
        private static bool IsSetup(TSqlStatement statement) => statement switch
        {
            SetOnOffStatement or SetRowCountStatement or SetTextSizeStatement or SetTransactionIsolationLevelStatement
                or SetErrorLevelStatement or SetCommandStatement => true,
            UseStatement or PrintStatement => true,
            DeclareVariableStatement declare => declare.Declarations.All(d => d.Value == null),
            _ => false
        };

        /// <summary>
        /// Groups the messages into statements.  A statement's messages come in a set order - its compile time, then
        /// its row count, its tables' I/O, and last its execution time - so its execution time ends it, and anything
        /// that can only begin a statement starts a new one: a compile time, a row count after the I/O lines (with
        /// STATISTICS TIME off), or a table already read.  A stored procedure's own execution time comes after its
        /// statements', as a statement of its own.
        /// </summary>
        public static StatisticsReport Parse(IEnumerable<string> lines)
        {
            var all = lines as IReadOnlyList<string> ?? lines.ToList();
            var report = new StatisticsReport();
            StatisticsStatement current = null;
            var pending = Pending.None;
            // Until the first row count, I/O or execution time: a compile before then is the query's own, done before it
            // ran rather than by something it ran.
            var seenOutput = false;
            // With STATISTICS TIME on, every statement ends with its execution time, so that's the only boundary needed -
            // a statement can read the same table twice (its plan reading it in two places), which without TIME would
            // have to be the next statement.
            var timed = all.Any(l => l.Contains(ExecutionHeader, StringComparison.OrdinalIgnoreCase));
            // An execution time just closed a statement, and nothing has come since.
            var afterExecution = false;

            // The next line with anything on it, from index i on - or null at the end.
            string NextLine(int i)
            {
                for (; i < all.Count; i++)
                {
                    var next = all[i].Trim();
                    if (next.Length > 0) return next;
                }
                return null;
            }

            void Close()
            {
                if (current is { IsEmpty: false })
                {
                    current.Number = report.Statements.Count + 1;
                    report.Statements.Add(current);
                }
                current = null;
            }

            StatisticsStatement Current() => current ??= new StatisticsStatement();

            for (var index = 0; index < all.Count; index++)
            {
                var line = all[index].Trim();
                if (line.Length == 0) continue;
                var justExecuted = afterExecution;
                afterExecution = false;

                if (line.StartsWith(CompileHeader, StringComparison.OrdinalIgnoreCase))
                {
                    pending = Pending.Compile;
                    line = line[CompileHeader.Length..].Trim();
                    if (line.Length == 0) continue; // The times are on the next line, as SSMS shows them.
                }
                else if (line.StartsWith(ExecutionHeader, StringComparison.OrdinalIgnoreCase))
                {
                    pending = Pending.Execution;
                    line = line[ExecutionHeader.Length..].Trim();
                    if (line.Length == 0)
                    {
                        // Only a header so far: the statement it ends hasn't been closed yet.
                        afterExecution = justExecuted;
                        continue;
                    }
                }

                var cpu = CpuLine.Match(line);
                if (cpu.Success)
                {
                    var cpuMs = long.Parse(cpu.Groups["cpu"].Value, CultureInfo.InvariantCulture);
                    var elapsedMs = long.Parse(cpu.Groups["elapsed"].Value, CultureInfo.InvariantCulture);
                    if (pending == Pending.Compile)
                    {
                        // Compiled before it runs, so it can't belong to a statement that already has output.
                        if (current is { HasOutput: true }) Close();
                        var statement = Current();
                        // A batch and then a statement in it can each be compiled before anything runs.
                        statement.CompileCpuMs = (statement.CompileCpuMs ?? 0) + cpuMs;
                        statement.CompileElapsedMs = (statement.CompileElapsedMs ?? 0) + elapsedMs;
                        if (!seenOutput)
                        {
                            report.CompileCpuMsBeforeOutput += cpuMs;
                            report.CompileElapsedMsBeforeOutput += elapsedMs;
                        }
                    }
                    else if (pending == Pending.Execution)
                    {
                        // A SET statement's 0 ms before the query proper doesn't count.
                        if (cpuMs > 0 || elapsedMs > 0) seenOutput = true;
                        var statement = Current();
                        statement.CpuMs = cpuMs;
                        statement.ElapsedMs = elapsedMs;
                        Close();
                        afterExecution = true;
                    }

                    pending = Pending.None;
                    continue;
                }

                pending = Pending.None;

                var rows = RowsAffected.Match(line);
                if (rows.Success)
                {
                    seenOutput = true;
                    var rowCount = long.Parse(rows.Groups["rows"].Value, CultureInfo.InvariantCulture);

                    // A data change prints its row count after its execution time - a SELECT before its I/O and time.
                    // So one straight after an execution time, not followed by I/O or a time of its own, is the count of
                    // the statement that time ended, rather than the start of the next.
                    if (timed && justExecuted && current == null && report.Statements is [.., { RowsAffected: null } last] &&
                        !last.IsTrivial && !StartsStatementOutput(NextLine(index + 1)))
                    {
                        last.RowsAffected = rowCount;
                        continue;
                    }

                    if (current != null && (current.RowsAffected != null || current.Tables.Count > 0)) Close();
                    Current().RowsAffected = rowCount;
                    continue;
                }

                if (TryParseTableLine(line, out var io))
                {
                    seenOutput = true;
                    var statement = Current();
                    var existing = statement.Tables.LastOrDefault(t => string.Equals(t.Table, io.Table, StringComparison.Ordinal));
                    var segmentLine = existing != null && !io.Counters.Keys.Any(existing.Counters.ContainsKey);

                    if (segmentLine)
                    {
                        // The columnstore segment line: it follows the table's own line with counters of its own.
                        foreach (var (name, value) in io.Counters) existing.Counters[name] = value;
                    }
                    else
                    {
                        // The same table again.  With STATISTICS TIME on it's the same statement reading it somewhere
                        // else in its plan - its execution time will end it.  Without, there's nothing else to go by, so
                        // it's taken as the next statement.
                        if (existing != null && !timed)
                        {
                            Close();
                            statement = Current();
                        }
                        statement.Tables.Add(io);
                    }

                    foreach (var name in io.Counters.Keys.Where(n => !report.CounterNames.Contains(n, StringComparer.OrdinalIgnoreCase)))
                    {
                        report.CounterNames.Add(name);
                    }
                    continue;
                }

                if (line.StartsWith(CompletionTimePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    report.CompletionTime = line[CompletionTimePrefix.Length..].Trim();
                    continue;
                }

                if (Error.IsMatch(line)) report.ErrorCount++;
            }

            Close();
            return report;
        }

        /// <summary>
        /// True for a line that's part of a statement's own output - its I/O, or its execution time - so a row count
        /// before it is that statement's, not the one before.
        /// </summary>
        private static bool StartsStatementOutput(string line) =>
            line != null && (line.StartsWith(ExecutionHeader, StringComparison.OrdinalIgnoreCase) || TableLine.IsMatch(line));

        /// <summary>A "Table '...'. Scan count 1, logical reads 2, ..." line, or false if this isn't one.</summary>
        public static bool TryParseTableLine(string line, out StatisticsTableIO io)
        {
            io = null;
            var match = TableLine.Match(line.Trim());
            if (!match.Success) return false;

            var parsed = new StatisticsTableIO(match.Groups["table"].Value);
            foreach (var part in match.Groups["counters"].Value.Split(','))
            {
                var counter = Counter.Match(part.Trim());
                if (!counter.Success) return false;
                if (!long.TryParse(counter.Groups["value"].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)) return false;
                parsed.Counters[CounterName(counter.Groups["name"].Value)] = value;
            }

            io = parsed;
            return true;
        }

        /// <summary>
        /// A counter's name as a column heading: "logical reads" as "Logical reads", "lob logical reads" as
        /// "LOB logical reads".
        /// </summary>
        public static string CounterName(string name)
        {
            name = Regex.Replace(name.Trim(), @"\s+", " ").ToLowerInvariant();
            name = Regex.Replace(name, @"\blob\b", "LOB");
            return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];
        }

        /// <summary>
        /// The counters worth a column: the core four always, any other only if something has a value for it - page
        /// server reads are only ever non-zero on Hyperscale, and LOB reads only for LOB data.
        /// </summary>
        public static List<string> ShownCounters(StatisticsReport report) =>
            CoreCounters.Concat(report.CounterNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(name => CoreCounters.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                               report.Statements.Any(s => s.Tables.Any(t => t.Counter(name) != 0)))
                .ToList();

        /// <summary>I/O totals for each table across every statement, most logical reads first.</summary>
        public static DataTable IOByTable(StatisticsReport report)
        {
            var counters = ShownCounters(report);
            var table = new DataTable("Table I/O");
            table.Columns.Add("Table", typeof(string));
            AddNumberColumn(table, "Statements");
            foreach (var name in counters) StatisticsBars.MoreIsWorse(AddNumberColumn(table, name));
            var share = table.Columns.Add("% Logical reads", typeof(decimal));
            share.ExtendedProperties["Format"] = "N1";
            StatisticsBars.MoreIsWorse(share, maximum: 100);

            var totalLogical = report.Counter(LogicalReads);
            var byTable = report.Statements
                .SelectMany(s => s.Tables.Select(t => (Statement: s, IO: t)))
                .GroupBy(x => x.IO.Table, StringComparer.Ordinal)
                .Select(g => (Table: g.Key,
                    Statements: g.Select(x => x.Statement).Distinct().Count(),
                    Totals: counters.ToDictionary(c => c, c => g.Sum(x => x.IO.Counter(c)), StringComparer.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.Totals[LogicalReads])
                .ThenBy(x => x.Table, StringComparer.OrdinalIgnoreCase);

            foreach (var (name, statements, totals) in byTable)
            {
                var row = table.NewRow();
                row["Table"] = name;
                row["Statements"] = (long)statements;
                foreach (var counter in counters) row[counter] = totals[counter];
                row["% Logical reads"] = totalLogical == 0 ? 0m : Math.Round(100m * totals[LogicalReads] / totalLogical, 1);
                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>Each Table line as it was printed, against the statement it belongs to.</summary>
        public static DataTable IOByStatement(StatisticsReport report)
        {
            var counters = ShownCounters(report);
            var table = new DataTable("Table I/O by statement");
            AddNumberColumn(table, "Statement");
            table.Columns.Add("Table", typeof(string));
            foreach (var name in counters) StatisticsBars.MoreIsWorse(AddNumberColumn(table, name));

            foreach (var statement in report.Statements)
            {
                foreach (var io in statement.Tables)
                {
                    var row = table.NewRow();
                    row["Statement"] = (long)statement.Number;
                    row["Table"] = io.Table;
                    foreach (var counter in counters) row[counter] = io.Counter(counter);
                    table.Rows.Add(row);
                }
            }

            return table;
        }

        /// <summary>Time, rows and reads for each statement.</summary>
        public static DataTable Statements(StatisticsReport report)
        {
            var table = new DataTable("Statements");
            AddNumberColumn(table, "Statement");
            StatisticsBars.Neutral(AddNumberColumn(table, "Rows affected"));
            StatisticsBars.MoreIsWorse(AddNumberColumn(table, "CPU ms"));
            StatisticsBars.MoreIsWorse(AddNumberColumn(table, "Elapsed ms"));
            StatisticsBars.MoreIsWorse(AddNumberColumn(table, "Compile CPU ms"));
            StatisticsBars.MoreIsWorse(AddNumberColumn(table, "Compile elapsed ms"));
            StatisticsBars.MoreIsWorse(AddNumberColumn(table, LogicalReads));
            StatisticsBars.MoreIsWorse(AddNumberColumn(table, PhysicalReads));
            StatisticsBars.MoreIsWorse(AddNumberColumn(table, ReadAheadReads));
            table.Columns.Add("Tables", typeof(string));
            // Only for a single statement that ran others, to tell its own time - the total - from theirs.
            var notes = report.TotalIncludesOthers;
            if (notes) table.Columns.Add("Note", typeof(string));

            foreach (var statement in report.Statements)
            {
                var row = table.NewRow();
                row["Statement"] = (long)statement.Number;
                row["Rows affected"] = (object)statement.RowsAffected ?? DBNull.Value;
                row["CPU ms"] = (object)statement.CpuMs ?? DBNull.Value;
                row["Elapsed ms"] = (object)statement.ElapsedMs ?? DBNull.Value;
                row["Compile CPU ms"] = (object)statement.CompileCpuMs ?? DBNull.Value;
                row["Compile elapsed ms"] = (object)statement.CompileElapsedMs ?? DBNull.Value;
                if (statement.Tables.Count > 0)
                {
                    row[LogicalReads] = statement.Counter(LogicalReads);
                    row[PhysicalReads] = statement.Counter(PhysicalReads);
                    row[ReadAheadReads] = statement.Counter(ReadAheadReads);
                    row["Tables"] = string.Join(", ", statement.Tables.Select(t => t.Table));
                }
                if (notes && statement.IsTotal)
                {
                    row["Note"] = report.SingleIsCall
                        ? "Total for the call - includes everything it ran"
                        : "Total for the statement - includes any triggers it fired";
                }
                table.Rows.Add(row);
            }

            return table;
        }

        private static DataColumn AddNumberColumn(DataTable table, string name)
        {
            var column = table.Columns.Add(name, typeof(long));
            column.ExtendedProperties["Format"] = "N0";
            return column;
        }
    }
}
