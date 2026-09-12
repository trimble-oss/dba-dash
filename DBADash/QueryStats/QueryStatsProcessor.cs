using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace DBADash.QueryStats
{
    /// <summary>
    /// How much of the plan cache to keep, and how much to roll up.  Every limit here is a ceiling on what
    /// travels to the repository rather than a filter on what is measured: the delta is calculated for
    /// every row that ran, and whatever these limits exclude is summed into a rollup row.  That ordering is
    /// what keeps "CPU for this interval" correct even when the per-query breakdown is truncated.
    /// </summary>
    public class QueryStatsLimits
    {
        /// <summary>Families kept per ranking measure.  Seven measures are ranked and the results unioned, so
        /// the family count is at least this and usually near it, because the same heavy queries top several
        /// measures at once.  Up to seven times it, plus any family kept for one slow execution.</summary>
        public int TopN = 50;

        /// <summary>Statements kept within a family before the rest become one OtherVariants row.  A family is
        /// usually one statement: the literal variants of an ad hoc query are one statement already, their
        /// shape, so a family only spreads where the same query sits in several procedures, or in a procedure
        /// and in ad hoc SQL.</summary>
        public int MaxStatementsPerFamily = 3;

        /// <summary>Plan shapes kept within a statement before the rest become one rolled up row.  Rarely
        /// reached: a statement usually runs under one plan shape in an interval.</summary>
        public int MaxPlansPerStatement = 5;

        /// <summary>An execution known to have taken at least this much elapsed time (microseconds) keeps its
        /// family regardless of ranking.  One expensive run a day is exactly the thing a top N by total
        /// would hide.  Known, because the DMV holds totals and a lifetime maximum rather than each
        /// execution: the average, the last execution and a maximum that rose in the interval are what can be
        /// placed in it, so a slow run among faster ones that set no new maximum goes unseen.</summary>
        public long SingleExecutionElapsedThreshold = 1000000;

        public static QueryStatsLimits Default => new();
    }

    /// <summary>
    /// The result of processing one read of the plan cache: what to send, and what to write into the
    /// baseline before it is sent.
    /// </summary>
    public class QueryStatsResult
    {
        public DataTable QueryStats;
        public DataTable Collection;

        /// <summary>
        /// The baseline as it should look after this collection.  Returned rather than applied, so that
        /// processing has no side effects; <see cref="Baseline.Apply"/> moves the baseline on, and the
        /// collector does that before the data is written - see <see cref="QueryStatsBaselineStore"/> for why
        /// not after.
        /// </summary>
        public List<KeyValuePair<BaselineKey, BaselineEntry>> PendingBaseline = new();

        public DateTime SnapshotDateUtc;
        public int ReadDurationMs;

        /// <summary>Rows the source query returned.</summary>
        public int RowsRead;

        /// <summary>
        /// The sql_handles of the statements this collection kept, heaviest first.
        ///
        /// <para>Only the statements that were stored: text for a query that was rolled up would be text for
        /// a row nobody can click on.  Ordered by cost so that a capped collection fetches the text that
        /// matters first and picks the rest up over later intervals, rather than fetching alphabetical
        /// luck.</para>
        ///
        /// <para>Not the ad hoc shapes', whose example handle is a different variant from one interval to the
        /// next: fetching each would store a new batch text per shape per interval.  They are in
        /// <see cref="Shapes"/> instead.</para>
        /// </summary>
        public List<byte[]> TextHandles = new();

        /// <summary>
        /// The ad hoc shapes this collection kept, heaviest first, each with the variant that stands as its
        /// example.  The collector fetches the example's text for the shapes it has not yet sent a template
        /// for, and writes the template onto the shape's rows.
        /// </summary>
        public List<ShapeExample> Shapes = new();

        /// <summary>
        /// The plan shapes this collection kept, heaviest first, each with the cache entry whose plan stands for
        /// it.  The collector fetches the plans it has not sent recently and writes each onto its row.  Only rows
        /// with a plan of their own: not the rollups, and not the plans past the per statement cap.
        /// </summary>
        public List<PlanExample> Plans = new();
    }

    /// <summary>
    /// A kept ad hoc shape and its example: the variant that did the most work in the interval.  The collector
    /// makes the shape's template from the example's statement text - see <see cref="QueryTemplate"/> - and
    /// sends both with <see cref="Rows"/>, once per shape rather than once per interval, so the work is left to
    /// it rather than done for every shape on every collection.
    /// </summary>
    public sealed class ShapeExample
    {
        public string DatabaseName;
        public byte[] QueryHash;
        public byte[] SqlHandle;
        public int StartOffset;
        public int EndOffset;

        /// <summary>The QUOTED_IDENTIFIER setting the example's plan compiled under, which decides whether a
        /// double quoted token in its text is an identifier or a string.</summary>
        public bool QuotedIdentifier;

        public long WorkerTime;

        /// <summary>The shape's rows in <see cref="QueryStatsResult.QueryStats"/>, one per plan shape.</summary>
        public List<DataRow> Rows;
    }

    /// <summary>
    /// A kept row's plan shape and the cache entry that did the most work under it in the interval.  Every entry
    /// under one query_plan_hash runs the same operators, so that entry's plan stands for the row - though its
    /// estimates and compiled values are its own, and for an ad hoc shape so are its statement text and literal
    /// values.  The collector sends it with <see cref="Row"/>, once per plan shape rather than once per interval,
    /// so which plans are worth fetching is left to it.
    /// </summary>
    public sealed class PlanExample
    {
        /// <summary>
        /// The statement, as the repository identifies it when it builds its StatementKey, with the database by
        /// name.  What the collector remembers a plan as sent by: a cache entry is no use for that, because a
        /// shape's heaviest entry is a different variant almost every interval.
        /// </summary>
        public string StatementKey;

        public byte[] PlanHash;
        public byte[] PlanHandle;
        public int StartOffset;
        public int EndOffset;

        /// <summary>The row's work in the interval, which orders the plans when a collection is capped.</summary>
        public long WorkerTime;

        /// <summary>The row in <see cref="QueryStatsResult.QueryStats"/> the plan belongs to.</summary>
        public DataRow Row;
    }

    /// <summary>
    /// Turns a plan cache snapshot into per-interval deltas: diff, rank by family, roll up the rest.
    ///
    /// <para>The order matters and is not interchangeable.  Diff first, at the grain the counters
    /// accumulate at, because a plan evicted between snapshots makes any pre-aggregated total fall.  Rank
    /// second, on the delta rather than the cumulative value, because a query that crosses a threshold
    /// between snapshots has no baseline and would read as a spike.  Roll up third, so the interval's
    /// totals survive the truncation.</para>
    /// </summary>
    public static class QueryStatsProcessor
    {
        /// <summary>
        /// Measures accumulate as a set.  The five version dependent ones are nullable: an instance that
        /// does not expose a counter contributes null rather than a zero that would read as "no spills"
        /// when the truth is "cannot tell".
        /// </summary>
        private class Measures
        {
            public long Executions;
            public long WorkerTime;
            public long ElapsedTime;
            public long LogicalReads;
            public long LogicalWrites;
            public long PhysicalReads;
            public long ClrTime;
            public long? Rows;
            public long? Dop;
            public long? GrantKb;
            public long? UsedGrantKb;
            public long? Spills;

            /// <summary>The slowest execution known to have finished in the interval, used by the threshold that
            /// keeps a family the ranking would drop.  A floor on the true slowest - see where it is set.</summary>
            public long SlowestExecution;

            public void Add(Measures m)
            {
                Executions += m.Executions;
                WorkerTime += m.WorkerTime;
                ElapsedTime += m.ElapsedTime;
                LogicalReads += m.LogicalReads;
                LogicalWrites += m.LogicalWrites;
                PhysicalReads += m.PhysicalReads;
                ClrTime += m.ClrTime;
                Rows = NullableAdd(Rows, m.Rows);
                Dop = NullableAdd(Dop, m.Dop);
                GrantKb = NullableAdd(GrantKb, m.GrantKb);
                UsedGrantKb = NullableAdd(UsedGrantKb, m.UsedGrantKb);
                Spills = NullableAdd(Spills, m.Spills);
                SlowestExecution = Math.Max(SlowestExecution, m.SlowestExecution);
            }

            /// <summary>
            /// Null means the instance does not expose the counter, which is not the same as zero, so adding
            /// an unsupported value makes the total unsupported too - we cannot total what we cannot see.
            /// A null accumulator, on the other hand, is just an empty one and starts from zero, otherwise
            /// every optional measure would stay null however many rows were added to it.
            /// </summary>
            private static long? NullableAdd(long? accumulated, long? value) =>
                value == null ? null : (accumulated ?? 0) + value;
        }

        /// <summary>One plan shape's delta within a statement.</summary>
        private class PlanDelta
        {
            public byte[] PlanHash;
            public readonly Measures Measures = new();
            public bool IsCompile;
            public int PlanCount;

            /// <summary>The cache entry that did the most work under the plan shape in the interval, whose plan
            /// stands for it if one is fetched - see <see cref="PlanExample"/>.</summary>
            public byte[] ExamplePlanHandle;

            public int ExampleStartOffset;
            public int ExampleEndOffset;
            public long ExampleWorkerTime = -1;
        }

        /// <summary>
        /// What identifies a statement - see <see cref="GetOrAddStatement"/>.  Value types rather than a string
        /// built per row, which allocated a key and a hex copy of the handle for every row with a delta only to
        /// look up one that usually existed already.
        /// </summary>
        private readonly record struct StatementId(string Database, string Schema, string Object, BaselineKey Handle,
            int StartOffset, int EndOffset, ulong QueryHash);

        /// <summary>A query shape within a database, or a statement of its own where it has no hash.</summary>
        private readonly record struct FamilyId(string Database, ulong QueryHash, StatementId? Statement);

        /// <summary>A plan shape.  A row with no plan hash is kept apart from one whose hash is zero.</summary>
        private readonly record struct PlanId(bool HasHash, ulong Hash);

        /// <summary>One statement's delta, broken down by plan shape.</summary>
        private class StatementDelta
        {
            public QueryStatsTables.StatementTypes StatementType;
            public string DatabaseName;
            public string SchemaName;
            public string ObjectName;
            public int? ObjectId;
            public byte[] SqlHandle;
            public int StartOffset;
            public int EndOffset;
            public byte[] QueryHash;
            public FamilyId Family;
            public readonly Dictionary<PlanId, PlanDelta> Plans = new();
            public readonly Measures Measures = new();

            /// <summary>For a shape, the work the example variant did, which a heavier variant replaces it for.</summary>
            public long ExampleWorkerTime;

            /// <summary>For a shape, the QUOTED_IDENTIFIER setting of the example's plan.</summary>
            public bool QuotedIdentifier = true;
        }

        /// <summary>
        /// Where each column is in the source query's result, found once rather than by name on every row.
        /// </summary>
        private sealed class Columns
        {
            public readonly int SnapshotDate, SqlHandle, StartOffset, EndOffset, PlanHandle, QueryHash, PlanHash,
                PlanGeneration, CreationTime, LastExecutionTime, LastElapsed, LastWorker, MaxElapsed, Executions,
                WorkerTime, ElapsedTime, LogicalReads, LogicalWrites, PhysicalReads, ClrTime, Rows, Dop, GrantKb,
                UsedGrantKb, Spills, DatabaseName, ObjectId, SchemaName, ObjectName, SetOptions;

            public Columns(IDataRecord record)
            {
                SnapshotDate = record.GetOrdinal("SnapshotDateUTC");
                SqlHandle = record.GetOrdinal("sql_handle");
                StartOffset = record.GetOrdinal("statement_start_offset");
                EndOffset = record.GetOrdinal("statement_end_offset");
                PlanHandle = record.GetOrdinal("plan_handle");
                QueryHash = record.GetOrdinal("query_hash");
                PlanHash = record.GetOrdinal("query_plan_hash");
                PlanGeneration = record.GetOrdinal("plan_generation_num");
                CreationTime = record.GetOrdinal("creation_time_utc");
                LastExecutionTime = record.GetOrdinal("last_execution_time_utc");
                LastElapsed = record.GetOrdinal("last_elapsed_time");
                LastWorker = record.GetOrdinal("last_worker_time");
                MaxElapsed = record.GetOrdinal("max_elapsed_time");
                Executions = record.GetOrdinal("execution_count");
                WorkerTime = record.GetOrdinal("total_worker_time");
                ElapsedTime = record.GetOrdinal("total_elapsed_time");
                LogicalReads = record.GetOrdinal("total_logical_reads");
                LogicalWrites = record.GetOrdinal("total_logical_writes");
                PhysicalReads = record.GetOrdinal("total_physical_reads");
                ClrTime = record.GetOrdinal("total_clr_time");
                Rows = record.GetOrdinal("total_rows");
                Dop = record.GetOrdinal("total_dop");
                GrantKb = record.GetOrdinal("total_grant_kb");
                UsedGrantKb = record.GetOrdinal("total_used_grant_kb");
                Spills = record.GetOrdinal("total_spills");
                DatabaseName = record.GetOrdinal("database_name");
                ObjectId = record.GetOrdinal("object_id");
                SchemaName = record.GetOrdinal("schema_name");
                ObjectName = record.GetOrdinal("object_name");
                SetOptions = record.GetOrdinal("set_options");
            }
        }

        /// <summary>The QUOTED_IDENTIFIER bit of a plan's set_options attribute.</summary>
        private const int QuotedIdentifierOption = 64;

        /// <summary>
        /// Buffers the handles and hashes are read into, reused for every row.  Most rows only need them long
        /// enough to be hashed into a key, so a copy is made only for the statements and plans that are kept.
        /// </summary>
        private sealed class RowBuffers
        {
            public readonly byte[] PlanHandle = new byte[64];
            public readonly byte[] SqlHandle = new byte[64];
            public readonly byte[] QueryHash = new byte[8];
            public readonly byte[] PlanHash = new byte[8];
        }

        private class FamilyDelta
        {
            public string DatabaseName;
            public byte[] QueryHash;
            public readonly List<StatementDelta> Statements = new();
            public readonly Measures Measures = new();
        }

        /// <summary>
        /// Diff the rows of the source query against the baseline as they are read, then rank and roll up.
        ///
        /// <para>Read straight from the reader rather than loaded into a table first.  A DataTable holds every
        /// row with all its columns, handles and names until the last is processed - tens of megabytes for a
        /// busy plan cache, on every collection of every instance - when most rows only need to be hashed and
        /// diffed, and only the statements with a delta are kept.</para>
        ///
        /// <para><paramref name="readStarted"/> is a <see cref="Stopwatch"/> timestamp taken when the command was
        /// sent.  The read is timed from it to the last row, so it includes the diffing done as rows arrive -
        /// a few microseconds a row, against the plan cache scan that dominates it.</para>
        /// </summary>
        public static QueryStatsResult Process(IDataReader reader, Baseline baseline, QueryStatsLimits limits,
            TimeSpan defaultInterval, long readStarted)
        {
            var result = new QueryStatsResult
            {
                QueryStats = QueryStatsTables.GetQueryStatsSchema(),
                Collection = QueryStatsTables.GetCollectionSchema()
            };

            var columns = new Columns(reader);
            var buffers = new RowBuffers();
            var hasRows = reader.Read();

            // Every row carries the same snapshot, and the interval it closes is needed before the first row
            // can be diffed
            var snapshotDate = hasRows ? reader.GetDateTime(columns.SnapshotDate) : DateTime.UtcNow;
            result.SnapshotDateUtc = snapshotDate;

            var intervalStart = baseline.LastCollectionUtc ?? snapshotDate.Subtract(defaultInterval);
            var periodTime = (long)snapshotDate.Subtract(intervalStart).TotalMilliseconds * 1000; // microseconds
            if (periodTime <= 0) periodTime = (long)defaultInterval.TotalMilliseconds * 1000;

            var statements = new Dictionary<StatementId, StatementDelta>();
            var rowsRead = 0;
            var rowsWithDelta = 0;
            var rowsUnattributed = 0;
            var unattributedWorker = 0L;
            var unattributedElapsed = 0L;
            var unattributedExecutions = 0L;

            for (; hasRows; hasRows = reader.Read())
            {
                rowsRead++;
                var planHandleLength = (int)reader.GetBytes(columns.PlanHandle, 0, buffers.PlanHandle, 0, buffers.PlanHandle.Length);
                var startOffset = reader.GetInt32(columns.StartOffset);
                var endOffset = reader.GetInt32(columns.EndOffset);
                var key = BaselineKey.Create(buffers.PlanHandle.AsSpan(0, planHandleLength), startOffset, endOffset);
                var current = ReadEntry(reader, columns, snapshotDate);

                // The most recently finished execution, which the source filter keeps inside the interval
                var lastElapsed = reader.GetInt64(columns.LastElapsed);
                var lastWorker = reader.GetInt64(columns.LastWorker);
                var lastFinished = reader.GetDateTime(columns.LastExecutionTime).Add(TimeSpan.FromMicroseconds(lastElapsed));
                var lastFinishedInInterval = lastFinished >= intervalStart;

                var found = baseline.TryGet(key, out var previous);
                var hasBaseline = found
                                  && previous.CreationTimeUtc == current.CreationTimeUtc
                                  && previous.PlanGenerationNum == current.PlanGenerationNum
                                  && NoCounterWentBackwards(previous, current);

                // Pending regardless of what happens to the delta: the row was seen, so the next
                // interval should diff against it even if this one could not attribute it.
                result.PendingBaseline.Add(new KeyValuePair<BaselineKey, BaselineEntry>(key, current));

                Measures delta;
                bool isCompile;
                // Everything the row holds was done inside the interval, so its maximum is the interval's too
                bool takenWhole;
                if (hasBaseline)
                {
                    delta = Subtract(current, previous);
                    isCompile = false;
                    takenWhole = false;
                }
                else if (current.CreationTimeUtc >= intervalStart)
                {
                    // Compiled inside the interval, so everything it has accumulated belongs to the interval.
                    delta = Subtract(current, null);
                    isCompile = true;
                    takenWhole = true;
                }
                else if ((!found || previous.CreationTimeUtc != current.CreationTimeUtc)
                         && baseline.WouldHaveSeen(current.CreationTimeUtc))
                {
                    // Compiled before the interval, but missing from a baseline that would hold it if it had
                    // finished anything earlier, so everything it has accumulated was done inside the interval.
                    // Typically a long execution that started before the previous read and finished after it -
                    // the run the single execution threshold is there to keep.  Not when only the generation
                    // number moved: a row seen at this compile time may still carry counters the previous
                    // delta took.
                    delta = Subtract(current, null);
                    isCompile = false;
                    takenWhole = true;
                }
                else
                {
                    // No baseline and compiled before the interval, so the counters can't be split by time and
                    // aren't presented as this interval's.  They are counted rather than dropped - as an estimate
                    // of the interval's share, not the plan's whole life in cache, which on a first collection
                    // would swamp the interval's own work.  The last execution finished inside the interval and
                    // counts in full; the earlier ones could have finished at any time since the plan compiled,
                    // so they are prorated over that time.  That is right for a job that runs once a day, whose
                    // earlier runs share nothing, and for a query running all day at a steady rate.
                    rowsUnattributed++;
                    if (lastFinishedInInterval)
                    {
                        var share = (lastFinished - intervalStart) / (lastFinished - current.CreationTimeUtc);
                        unattributedWorker += lastWorker + Prorate(current.WorkerTime - lastWorker, share);
                        unattributedElapsed += lastElapsed + Prorate(current.ElapsedTime - lastElapsed, share);
                        unattributedExecutions += 1 + Prorate(current.ExecutionCount - 1, share);
                    }
                    continue;
                }

                if (delta.Executions <= 0) continue;
                rowsWithDelta++;

                // The slowest execution known to have finished inside the interval.  The average is a floor on
                // it, the last execution finished inside the interval, and the plan's maximum is the interval's
                // where everything the row holds was done here or where it has risen since the previous
                // collection.  The DMV has no way to show a slow execution among faster ones that set no new
                // maximum - the Slow Queries collection is what records every one of those.
                var slowest = delta.ElapsedTime / delta.Executions;
                if (lastFinishedInInterval) slowest = Math.Max(slowest, lastElapsed);
                if (takenWhole || current.MaxElapsedTime > previous.MaxElapsedTime)
                {
                    slowest = Math.Max(slowest, current.MaxElapsedTime);
                }
                delta.SlowestExecution = slowest;

                var sqlHandleLength = (int)reader.GetBytes(columns.SqlHandle, 0, buffers.SqlHandle, 0, buffers.SqlHandle.Length);
                var statement = GetOrAddStatement(statements, reader, columns, buffers, sqlHandleLength, startOffset,
                    endOffset, out var added);
                statement.Measures.Add(delta);

                // A shape's example is the variant that did the most work in the interval - the text a reader is
                // most likely to have gone looking for, though still only one of the shape's.
                if (statement.StatementType == QueryStatsTables.StatementTypes.AdHocShape
                    && (added || delta.WorkerTime > statement.ExampleWorkerTime))
                {
                    statement.SqlHandle = buffers.SqlHandle.AsSpan(0, sqlHandleLength).ToArray();
                    statement.StartOffset = startOffset;
                    statement.EndOffset = endOffset;
                    statement.ExampleWorkerTime = delta.WorkerTime;
                    statement.QuotedIdentifier = reader.IsDBNull(columns.SetOptions)
                                                 || (reader.GetInt32(columns.SetOptions) & QuotedIdentifierOption) != 0;
                }

                var hasPlanHash = !reader.IsDBNull(columns.PlanHash);
                var planId = hasPlanHash
                    ? new PlanId(true, ReadHash(reader, columns.PlanHash, buffers.PlanHash))
                    : new PlanId(false, 0);
                if (!statement.Plans.TryGetValue(planId, out var plan))
                {
                    plan = new PlanDelta { PlanHash = hasPlanHash ? (byte[])buffers.PlanHash.Clone() : Array.Empty<byte>() };
                    statement.Plans.Add(planId, plan);
                }
                plan.Measures.Add(delta);
                plan.PlanCount++;
                plan.IsCompile |= isCompile;

                // The entry whose plan would stand for the plan shape: the one that did the most work.  Copied into
                // the buffer it already has where the handle is the same length, which a plan handle always is in
                // practice, so a shape with thousands of entries does not allocate for each heavier one it meets.
                if (delta.WorkerTime > plan.ExampleWorkerTime)
                {
                    if (plan.ExamplePlanHandle?.Length != planHandleLength)
                    {
                        plan.ExamplePlanHandle = new byte[planHandleLength];
                    }
                    buffers.PlanHandle.AsSpan(0, planHandleLength).CopyTo(plan.ExamplePlanHandle);
                    plan.ExampleStartOffset = startOffset;
                    plan.ExampleEndOffset = endOffset;
                    plan.ExampleWorkerTime = delta.WorkerTime;
                }
            }

            result.ReadDurationMs = (int)Stopwatch.GetElapsedTime(readStarted).TotalMilliseconds;
            result.RowsRead = rowsRead;
            var sw = Stopwatch.StartNew();

            var families = BuildFamilies(statements.Values);
            var kept = RankFamilies(families, limits);

            var statementsKept = 0;
            var otherQueries = new Dictionary<string, Measures>(StringComparer.Ordinal);
            var keptHandles = new List<(long WorkerTime, byte[] Handle)>();

            foreach (var family in families.Values)
            {
                if (!kept.Contains(family))
                {
                    // Not interesting enough to keep in detail, but its work still belongs to the interval.
                    var dbKey = family.DatabaseName ?? string.Empty;
                    if (!otherQueries.TryGetValue(dbKey, out var other))
                    {
                        other = new Measures();
                        otherQueries.Add(dbKey, other);
                    }
                    other.Add(family.Measures);
                    continue;
                }

                var ordered = family.Statements.OrderByDescending(s => s.Measures.WorkerTime).ToList();
                foreach (var statement in ordered.Take(limits.MaxStatementsPerFamily))
                {
                    statementsKept++;
                    var rows = WriteStatement(result.QueryStats, statement, snapshotDate, periodTime, limits, result.Plans);
                    if (statement.StatementType == QueryStatsTables.StatementTypes.AdHocShape)
                    {
                        result.Shapes.Add(new ShapeExample
                        {
                            DatabaseName = statement.DatabaseName,
                            QueryHash = statement.QueryHash,
                            SqlHandle = statement.SqlHandle,
                            StartOffset = statement.StartOffset,
                            EndOffset = statement.EndOffset,
                            QuotedIdentifier = statement.QuotedIdentifier,
                            WorkerTime = statement.Measures.WorkerTime,
                            Rows = rows
                        });
                    }
                    else if (statement.SqlHandle is { Length: > 0 })
                    {
                        keptHandles.Add((statement.Measures.WorkerTime, statement.SqlHandle));
                    }
                }

                var variants = ordered.Skip(limits.MaxStatementsPerFamily).ToList();
                if (variants.Count == 0) continue;

                var rolled = new Measures();
                foreach (var statement in variants)
                {
                    rolled.Add(statement.Measures);
                }
                WriteRollup(result.QueryStats, QueryStatsTables.StatementTypes.OtherVariants, family.DatabaseName,
                    family.QueryHash, rolled, snapshotDate, periodTime, variants.Count);
            }

            // One rollup row per database, for the databases with the most work in theirs, and one row for the rest.
            // Capped at the top N, like the families: the rows scale with the number of databases otherwise, which
            // on an instance hosting thousands of them would be most of what the collection writes.
            var rankedDatabases = otherQueries
                .OrderByDescending(kv => kv.Value.WorkerTime)
                .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();
            foreach (var kv in rankedDatabases.Take(limits.TopN))
            {
                WriteRollup(result.QueryStats, QueryStatsTables.StatementTypes.OtherQueries,
                    kv.Key.Length == 0 ? null : kv.Key, null, kv.Value, snapshotDate, periodTime, 0);
            }
            if (rankedDatabases.Count > limits.TopN)
            {
                var otherDatabases = new Measures();
                foreach (var kv in rankedDatabases.Skip(limits.TopN))
                {
                    otherDatabases.Add(kv.Value);
                }
                WriteRollup(result.QueryStats, QueryStatsTables.StatementTypes.OtherDatabases, null, null,
                    otherDatabases, snapshotDate, periodTime, 0);
            }

            // One handle per batch, not per statement: several statements of one procedure share its handle,
            // and the text that gets fetched is the whole batch either way.
            result.TextHandles = keptHandles
                .GroupBy(h => h.Handle.ToHexString(), StringComparer.Ordinal)
                .Select(g => (WorkerTime: g.Max(h => h.WorkerTime), g.First().Handle))
                .OrderByDescending(h => h.WorkerTime)
                .Select(h => h.Handle)
                .ToList();
            result.Shapes.Sort((a, b) => b.WorkerTime.CompareTo(a.WorkerTime));
            result.Plans.Sort((a, b) => b.WorkerTime.CompareTo(a.WorkerTime));

            sw.Stop();

            var collectionRow = result.Collection.NewRow();
            collectionRow["SnapshotDate"] = snapshotDate;
            collectionRow["PeriodTime"] = periodTime;
            collectionRow["DMVRowCount"] = rowsRead;
            collectionRow["RowsWithDelta"] = rowsWithDelta;
            collectionRow["RowsUnattributed"] = rowsUnattributed;
            collectionRow["UnattributedWorkerTime"] = unattributedWorker;
            collectionRow["UnattributedElapsedTime"] = unattributedElapsed;
            collectionRow["UnattributedExecutions"] = unattributedExecutions;
            collectionRow["FamilyCount"] = families.Count;
            collectionRow["FamiliesKept"] = kept.Count;
            collectionRow["StatementsKept"] = statementsKept;
            collectionRow["RowsPersisted"] = result.QueryStats.Rows.Count;
            collectionRow["BaselineCount"] = baseline.Count;
            collectionRow["BaselineEvictions"] = baseline.Evictions;
            collectionRow["ReadDurationMs"] = result.ReadDurationMs;
            collectionRow["ProcessingDurationMs"] = (int)sw.ElapsedMilliseconds;
            collectionRow["IsFirstCollection"] = baseline.IsFirstCollection;
            collectionRow["IsSkipped"] = false;
            result.Collection.Rows.Add(collectionRow);

            return result;
        }

        /// <summary>
        /// The collection-stats row for an interval that was deliberately not read, so a skipped interval
        /// is visible in the repository rather than looking like an outage.
        /// </summary>
        public static DataTable GetSkippedCollection(DateTime snapshotDate, long periodTime, int lastReadDurationMs)
        {
            var dt = QueryStatsTables.GetCollectionSchema();
            var row = dt.NewRow();
            row["SnapshotDate"] = snapshotDate;
            row["PeriodTime"] = periodTime;
            row["DMVRowCount"] = 0;
            row["RowsWithDelta"] = 0;
            row["RowsUnattributed"] = 0;
            row["UnattributedWorkerTime"] = 0L;
            row["UnattributedElapsedTime"] = 0L;
            row["UnattributedExecutions"] = 0L;
            row["FamilyCount"] = 0;
            row["FamiliesKept"] = 0;
            row["StatementsKept"] = 0;
            row["RowsPersisted"] = 0;
            row["BaselineCount"] = 0;
            row["BaselineEvictions"] = 0;
            row["ReadDurationMs"] = lastReadDurationMs;
            row["ProcessingDurationMs"] = 0;
            row["IsFirstCollection"] = false;
            row["IsSkipped"] = true;
            dt.Rows.Add(row);
            return dt;
        }

        /// <param name="added">Whether the statement was created by this row rather than found.</param>
        private static StatementDelta GetOrAddStatement(Dictionary<StatementId, StatementDelta> statements,
            IDataRecord record, Columns columns, RowBuffers buffers, int sqlHandleLength, int startOffset,
            int endOffset, out bool added)
        {
            var databaseName = GetNullableString(record, columns.DatabaseName);
            var objectName = GetNullableString(record, columns.ObjectName);
            var schemaName = GetNullableString(record, columns.SchemaName);
            var sqlHandle = buffers.SqlHandle.AsSpan(0, sqlHandleLength);
            var isModule = !string.IsNullOrEmpty(objectName);
            var hasQueryHash = !record.IsDBNull(columns.QueryHash);
            // Read for every ad hoc row, because it identifies them.  A module statement only needs it once.
            var queryHash = hasQueryHash && !isModule ? ReadHash(record, columns.QueryHash, buffers.QueryHash) : 0;
            var isShape = !isModule && queryHash != 0;

            // Module statements are identified by the object rather than the handle: a module sql_handle
            // contains the object_id, which changes when the object is dropped and recreated, so a handle
            // keyed history splits at every redeploy.  Ad hoc statements are identified by their shape rather
            // than the handle: an ad hoc sql_handle is a hash of the batch text, so every literal value makes a
            // new one, and a query that sends its values as literals would otherwise be a new statement for
            // each - a history of single points, with a statement and a batch text stored for every value.
            // Only a statement with no hash to identify it by falls back to its handle.
            var id = isModule ? new StatementId(databaseName, schemaName, objectName, default, startOffset, endOffset, 0)
                : isShape ? new StatementId(databaseName, null, null, default, 0, 0, queryHash)
                : new StatementId(databaseName, null, null, BaselineKey.Create(sqlHandle, startOffset, endOffset),
                    startOffset, endOffset, 0);

            added = false;
            if (statements.TryGetValue(id, out var statement)) return statement;

            added = true;
            if (isModule && hasQueryHash) queryHash = ReadHash(record, columns.QueryHash, buffers.QueryHash);
            statement = new StatementDelta
            {
                StatementType = isModule ? QueryStatsTables.StatementTypes.Module
                    : isShape ? QueryStatsTables.StatementTypes.AdHocShape
                    : QueryStatsTables.StatementTypes.AdHoc,
                DatabaseName = databaseName,
                SchemaName = schemaName,
                ObjectName = objectName,
                ObjectId = record.IsDBNull(columns.ObjectId) ? null : record.GetInt32(columns.ObjectId),
                // A shape's handle and offsets are its example's, which the caller sets
                SqlHandle = isShape ? null : sqlHandle.ToArray(),
                StartOffset = startOffset,
                EndOffset = endOffset,
                QueryHash = hasQueryHash ? (byte[])buffers.QueryHash.Clone() : null,
                // A family is a query shape within a database.  A statement whose hash is zero - some
                // statement types have no hash - is its own family rather than being lumped in with every
                // other hashless statement.
                Family = queryHash == 0
                    ? new FamilyId(databaseName, 0, id)
                    : new FamilyId(databaseName, queryHash, null)
            };
            statements.Add(id, statement);
            return statement;
        }

        private static Dictionary<FamilyId, FamilyDelta> BuildFamilies(IEnumerable<StatementDelta> statements)
        {
            var families = new Dictionary<FamilyId, FamilyDelta>();
            foreach (var statement in statements)
            {
                if (!families.TryGetValue(statement.Family, out var family))
                {
                    family = new FamilyDelta
                    {
                        DatabaseName = statement.DatabaseName,
                        QueryHash = statement.QueryHash
                    };
                    families.Add(statement.Family, family);
                }
                family.Statements.Add(statement);
                family.Measures.Add(statement.Measures);
            }
            return families;
        }

        /// <summary>
        /// Top N by each measure, unioned, plus anything with one expensive execution.  Ranking on several
        /// measures rather than one is what stops a write heavy or memory hungry query from being invisible
        /// because it happens not to use much CPU.
        /// </summary>
        private static HashSet<FamilyDelta> RankFamilies(Dictionary<FamilyId, FamilyDelta> families, QueryStatsLimits limits)
        {
            var kept = new HashSet<FamilyDelta>();
            var all = families.Values.ToList();

            var measures = new Func<FamilyDelta, long>[]
            {
                f => f.Measures.WorkerTime,
                f => f.Measures.ElapsedTime,
                f => f.Measures.LogicalReads,
                f => f.Measures.LogicalWrites,
                f => f.Measures.PhysicalReads,
                f => f.Measures.Executions,
                // Grant per execution rather than the total, which grows with how often a query runs and so
                // would rank a small grant executed often above a large one executed once.
                f => f.Measures.Executions == 0 ? 0 : (f.Measures.GrantKb ?? 0) / f.Measures.Executions
            };

            foreach (var measure in measures)
            {
                foreach (var family in all.OrderByDescending(measure).Take(limits.TopN))
                {
                    if (measure(family) > 0) kept.Add(family);
                }
            }

            foreach (var family in all.Where(f => f.Measures.SlowestExecution >= limits.SingleExecutionElapsedThreshold))
            {
                kept.Add(family);
            }

            return kept;
        }

        /// <param name="examples">Where the plan shapes written are added, each with the cache entry whose plan
        /// stands for it.</param>
        /// <returns>The rows written, one per plan shape kept plus any rollup of the rest.</returns>
        private static List<DataRow> WriteStatement(DataTable dt, StatementDelta statement, DateTime snapshotDate,
            long periodTime, QueryStatsLimits limits, List<PlanExample> examples)
        {
            var plans = statement.Plans.Values.OrderByDescending(p => p.Measures.WorkerTime).ToList();
            var rows = new List<DataRow>();
            string statementKey = null;

            foreach (var plan in plans.Take(limits.MaxPlansPerStatement))
            {
                var row = NewStatementRow(dt, statement, snapshotDate, periodTime);
                row["query_plan_hash"] = plan.PlanHash.Length == 0 ? (object)DBNull.Value : plan.PlanHash;
                SetMeasures(row, plan.Measures);
                row["PlanCount"] = plan.PlanCount;
                row["IsCompile"] = plan.IsCompile;
                row["IsOtherPlans"] = false;
                dt.Rows.Add(row);
                rows.Add(row);

                // A hash of zeros is what a statement with no plan of its own reports, so there is nothing to fetch
                if (plan.ExamplePlanHandle == null || !plan.PlanHash.Any(b => b != 0)) continue;
                examples.Add(new PlanExample
                {
                    StatementKey = statementKey ??= GetStatementKey(statement),
                    PlanHash = plan.PlanHash,
                    PlanHandle = plan.ExamplePlanHandle,
                    StartOffset = plan.ExampleStartOffset,
                    EndOffset = plan.ExampleEndOffset,
                    WorkerTime = plan.Measures.WorkerTime,
                    Row = row
                });
            }

            var extra = plans.Skip(limits.MaxPlansPerStatement).ToList();
            if (extra.Count == 0) return rows;

            var rolled = new Measures();
            var planCount = 0;
            var isCompile = false;
            foreach (var plan in extra)
            {
                rolled.Add(plan.Measures);
                planCount += plan.PlanCount;
                isCompile |= plan.IsCompile;
            }

            var rollupRow = NewStatementRow(dt, statement, snapshotDate, periodTime);
            rollupRow["query_plan_hash"] = DBNull.Value;
            SetMeasures(rollupRow, rolled);
            rollupRow["PlanCount"] = planCount;
            rollupRow["IsCompile"] = isCompile;
            rollupRow["IsOtherPlans"] = true;
            dt.Rows.Add(rollupRow);
            rows.Add(rollupRow);
            return rows;
        }

        /// <summary>
        /// The statement's identity, from the same parts dbo.QueryStats_Upd builds its StatementKey from, with the
        /// database by name rather than by the repository's id for it.
        /// </summary>
        private static string GetStatementKey(StatementDelta statement) => statement.StatementType switch
        {
            QueryStatsTables.StatementTypes.Module =>
                $"1|{statement.DatabaseName}|{statement.SchemaName}|{statement.ObjectName}|{statement.StartOffset}|{statement.EndOffset}",
            QueryStatsTables.StatementTypes.AdHocShape =>
                $"5|{statement.DatabaseName}|{statement.QueryHash.ToHexString()}",
            _ => $"0|{statement.DatabaseName}|{statement.SqlHandle.ToHexString()}|{statement.StartOffset}|{statement.EndOffset}"
        };

        private static DataRow NewStatementRow(DataTable dt, StatementDelta statement, DateTime snapshotDate, long periodTime)
        {
            var row = dt.NewRow();
            row["StatementType"] = (byte)statement.StatementType;
            row["database_name"] = statement.DatabaseName ?? (object)DBNull.Value;
            row["schema_name"] = statement.SchemaName ?? (object)DBNull.Value;
            row["object_name"] = statement.ObjectName ?? (object)DBNull.Value;
            row["object_id"] = statement.ObjectId ?? (object)DBNull.Value;
            row["sql_handle"] = statement.SqlHandle ?? (object)DBNull.Value;
            row["statement_start_offset"] = statement.StartOffset;
            row["statement_end_offset"] = statement.EndOffset;
            row["query_hash"] = statement.QueryHash ?? (object)DBNull.Value;
            row["SnapshotDate"] = snapshotDate;
            row["PeriodTime"] = periodTime;
            return row;
        }

        private static void WriteRollup(DataTable dt, QueryStatsTables.StatementTypes type, string databaseName,
            byte[] queryHash, Measures measures, DateTime snapshotDate, long periodTime, int statementCount)
        {
            var row = dt.NewRow();
            row["StatementType"] = (byte)type;
            row["database_name"] = databaseName ?? (object)DBNull.Value;
            row["schema_name"] = DBNull.Value;
            row["object_name"] = DBNull.Value;
            row["object_id"] = DBNull.Value;
            row["sql_handle"] = DBNull.Value;
            // Rollup rows have no offsets of their own.  Zero rather than null keeps the dimension's
            // unique keys simple, since the rollup types are separated by StatementType anyway.
            row["statement_start_offset"] = 0;
            row["statement_end_offset"] = 0;
            row["query_hash"] = queryHash ?? (object)DBNull.Value;
            row["query_plan_hash"] = DBNull.Value;
            row["SnapshotDate"] = snapshotDate;
            row["PeriodTime"] = periodTime;
            SetMeasures(row, measures);
            row["PlanCount"] = statementCount;
            row["IsCompile"] = false;
            row["IsOtherPlans"] = false;
            dt.Rows.Add(row);
        }

        private static void SetMeasures(DataRow row, Measures m)
        {
            row["execution_count"] = m.Executions;
            row["total_worker_time"] = m.WorkerTime;
            row["total_elapsed_time"] = m.ElapsedTime;
            row["total_logical_reads"] = m.LogicalReads;
            row["total_logical_writes"] = m.LogicalWrites;
            row["total_physical_reads"] = m.PhysicalReads;
            row["total_clr_time"] = m.ClrTime;
            row["total_rows"] = m.Rows ?? (object)DBNull.Value;
            row["total_dop"] = m.Dop ?? (object)DBNull.Value;
            row["total_grant_kb"] = m.GrantKb ?? (object)DBNull.Value;
            row["total_used_grant_kb"] = m.UsedGrantKb ?? (object)DBNull.Value;
            row["total_spills"] = m.Spills ?? (object)DBNull.Value;
        }

        private static BaselineEntry ReadEntry(IDataRecord record, Columns columns, DateTime snapshotDate) => new()
        {
            ExecutionCount = record.GetInt64(columns.Executions),
            WorkerTime = record.GetInt64(columns.WorkerTime),
            ElapsedTime = record.GetInt64(columns.ElapsedTime),
            LogicalReads = record.GetInt64(columns.LogicalReads),
            LogicalWrites = record.GetInt64(columns.LogicalWrites),
            PhysicalReads = record.GetInt64(columns.PhysicalReads),
            ClrTime = record.GetInt64(columns.ClrTime),
            Rows = GetOptional(record, columns.Rows),
            Dop = GetOptional(record, columns.Dop),
            GrantKb = GetOptional(record, columns.GrantKb),
            UsedGrantKb = GetOptional(record, columns.UsedGrantKb),
            Spills = GetOptional(record, columns.Spills),
            MaxElapsedTime = record.GetInt64(columns.MaxElapsed),
            CreationTimeUtc = record.GetDateTime(columns.CreationTime),
            PlanGenerationNum = record.GetInt64(columns.PlanGeneration),
            LastSeenTicks = snapshotDate.Ticks
        };

        private static long GetOptional(IDataRecord record, int ordinal) =>
            record.IsDBNull(ordinal) ? BaselineEntry.NotSupported : record.GetInt64(ordinal);

        private static string GetNullableString(IDataRecord record, int ordinal) =>
            record.IsDBNull(ordinal) ? null : record.GetString(ordinal);

        /// <summary>An eight byte hash, read into <paramref name="buffer"/> and returned as a number to key on.</summary>
        private static ulong ReadHash(IDataRecord record, int ordinal, byte[] buffer)
        {
            var length = (int)record.GetBytes(ordinal, 0, buffer, 0, buffer.Length);
            return length == buffer.Length ? BinaryPrimitives.ReadUInt64BigEndian(buffer) : 0;
        }

        /// <summary>The part of <paramref name="amount"/> estimated to fall inside the interval.</summary>
        private static long Prorate(long amount, double share) =>
            amount <= 0 ? 0 : (long)Math.Round(amount * share);

        /// <summary>
        /// A counter that has gone backwards means the row was reset even though the compile time and
        /// generation number look unchanged, so the difference is not a delta and must not be treated as one.
        /// </summary>
        private static bool NoCounterWentBackwards(BaselineEntry previous, BaselineEntry current) =>
            current.ExecutionCount >= previous.ExecutionCount
            && current.WorkerTime >= previous.WorkerTime
            && current.ElapsedTime >= previous.ElapsedTime
            && current.LogicalReads >= previous.LogicalReads
            && current.LogicalWrites >= previous.LogicalWrites
            && current.PhysicalReads >= previous.PhysicalReads;

        private static Measures Subtract(BaselineEntry current, BaselineEntry? previous)
        {
            var p = previous ?? new BaselineEntry
            {
                Rows = current.Rows == BaselineEntry.NotSupported ? BaselineEntry.NotSupported : 0,
                Dop = current.Dop == BaselineEntry.NotSupported ? BaselineEntry.NotSupported : 0,
                GrantKb = current.GrantKb == BaselineEntry.NotSupported ? BaselineEntry.NotSupported : 0,
                UsedGrantKb = current.UsedGrantKb == BaselineEntry.NotSupported ? BaselineEntry.NotSupported : 0,
                Spills = current.Spills == BaselineEntry.NotSupported ? BaselineEntry.NotSupported : 0
            };

            return new Measures
            {
                Executions = current.ExecutionCount - p.ExecutionCount,
                WorkerTime = current.WorkerTime - p.WorkerTime,
                ElapsedTime = current.ElapsedTime - p.ElapsedTime,
                LogicalReads = current.LogicalReads - p.LogicalReads,
                LogicalWrites = current.LogicalWrites - p.LogicalWrites,
                PhysicalReads = current.PhysicalReads - p.PhysicalReads,
                ClrTime = Math.Max(0, current.ClrTime - p.ClrTime),
                Rows = OptionalDelta(current.Rows, p.Rows),
                Dop = OptionalDelta(current.Dop, p.Dop),
                GrantKb = OptionalDelta(current.GrantKb, p.GrantKb),
                UsedGrantKb = OptionalDelta(current.UsedGrantKb, p.UsedGrantKb),
                Spills = OptionalDelta(current.Spills, p.Spills)
            };
        }

        private static long? OptionalDelta(long current, long previous)
        {
            if (current == BaselineEntry.NotSupported) return null;
            if (previous == BaselineEntry.NotSupported) return current;
            return Math.Max(0, current - previous);
        }
    }
}
