using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DBADash.QueryStats;
using DBADashService;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The query stats diff, which is the part of the collection that can be quietly wrong.
    ///
    /// <para>The claim these tests protect is the one the whole design rests on: what is stored for an
    /// interval, detail plus rollups, adds up to the work the instance actually did in that interval, and
    /// anything that could not be attributed is counted rather than dropped.</para>
    /// </summary>
    [TestClass]
    public class QueryStatsProcessorTests
    {
        private static readonly DateTime Snapshot = new(2026, 9, 12, 10, 5, 0, DateTimeKind.Utc);
        private static readonly DateTime PreviousCollection = new(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

        private static DataTable NewRawTable()
        {
            var dt = new DataTable("QueryStatsRaw");
            dt.Columns.Add("SnapshotDateUTC", typeof(DateTime));
            dt.Columns.Add("sql_handle", typeof(byte[]));
            dt.Columns.Add("statement_start_offset", typeof(int));
            dt.Columns.Add("statement_end_offset", typeof(int));
            dt.Columns.Add("plan_handle", typeof(byte[]));
            dt.Columns.Add("query_hash", typeof(byte[]));
            dt.Columns.Add("query_plan_hash", typeof(byte[]));
            dt.Columns.Add("plan_generation_num", typeof(long));
            dt.Columns.Add("creation_time_utc", typeof(DateTime));
            dt.Columns.Add("last_execution_time_utc", typeof(DateTime));
            dt.Columns.Add("last_elapsed_time", typeof(long));
            dt.Columns.Add("last_worker_time", typeof(long));
            dt.Columns.Add("max_elapsed_time", typeof(long));
            dt.Columns.Add("execution_count", typeof(long));
            dt.Columns.Add("total_worker_time", typeof(long));
            dt.Columns.Add("total_elapsed_time", typeof(long));
            dt.Columns.Add("total_logical_reads", typeof(long));
            dt.Columns.Add("total_logical_writes", typeof(long));
            dt.Columns.Add("total_physical_reads", typeof(long));
            dt.Columns.Add("total_clr_time", typeof(long));
            dt.Columns.Add("total_rows", typeof(long));
            dt.Columns.Add("total_dop", typeof(long));
            dt.Columns.Add("total_grant_kb", typeof(long));
            dt.Columns.Add("total_used_grant_kb", typeof(long));
            dt.Columns.Add("total_spills", typeof(long));
            dt.Columns.Add("dbid", typeof(int));
            dt.Columns.Add("database_name", typeof(string));
            dt.Columns.Add("object_id", typeof(int));
            dt.Columns.Add("schema_name", typeof(string));
            dt.Columns.Add("object_name", typeof(string));
            dt.Columns.Add("set_options", typeof(int));
            return dt;
        }

        /// <summary>A typical client's set_options: ANSI settings, ARITHABORT and QUOTED_IDENTIFIER (64) on.</summary>
        private const int DefaultSetOptions = 4347;

        /// <param name="planHandle">Identity for diffing purposes - rows sharing one are the same cached plan.</param>
        /// <param name="lastElapsed">The most recent execution, which finishes at the snapshot.  Defaults to the
        /// average execution, as every execution in the fixture is alike unless a test says otherwise.</param>
        /// <param name="maxElapsed">The plan's slowest execution.  Defaults to the last one.</param>
        private static DataRow AddRow(DataTable dt, string planHandle, long executions, long workerTime,
            DateTime? creationTime = null, string database = "AppDB", string? objectName = null,
            string queryHash = "AAAAAAAAAAAAAAAA", string planHash = "1111111111111111",
            int startOffset = 0, int endOffset = 100, long planGeneration = 1, string? sqlHandle = null,
            long? lastElapsed = null, long? maxElapsed = null, int setOptions = DefaultSetOptions)
        {
            var lastWorker = executions > 0 ? workerTime / executions : 0;
            var last = lastElapsed ?? lastWorker * 2;
            var row = dt.NewRow();
            row["SnapshotDateUTC"] = Snapshot;
            row["sql_handle"] = Convert.FromHexString(sqlHandle ?? (objectName == null ? "0200000011" : "0300050011"));
            row["statement_start_offset"] = startOffset;
            row["statement_end_offset"] = endOffset;
            row["plan_handle"] = Convert.FromHexString(planHandle);
            row["query_hash"] = Convert.FromHexString(queryHash);
            row["query_plan_hash"] = Convert.FromHexString(planHash);
            row["plan_generation_num"] = planGeneration;
            row["creation_time_utc"] = creationTime ?? PreviousCollection.AddHours(-3);
            row["last_execution_time_utc"] = Snapshot.Subtract(TimeSpan.FromMicroseconds(last));
            row["last_elapsed_time"] = last;
            row["last_worker_time"] = lastWorker;
            row["max_elapsed_time"] = maxElapsed ?? last;
            row["execution_count"] = executions;
            row["total_worker_time"] = workerTime;
            row["total_elapsed_time"] = workerTime * 2;
            row["total_logical_reads"] = executions * 10;
            row["total_logical_writes"] = 0L;
            row["total_physical_reads"] = 0L;
            row["total_clr_time"] = 0L;
            row["total_rows"] = executions;
            row["total_dop"] = executions;
            row["total_grant_kb"] = 0L;
            row["total_used_grant_kb"] = 0L;
            row["total_spills"] = 0L;
            row["dbid"] = 5;
            row["database_name"] = database;
            row["object_id"] = objectName == null ? DBNull.Value : 111;
            row["schema_name"] = objectName == null ? (object)DBNull.Value : "dbo";
            row["object_name"] = objectName ?? (object)DBNull.Value;
            row["set_options"] = setOptions;
            dt.Rows.Add(row);
            return row;
        }

        private static Baseline NewBaseline(bool withPreviousCollection = true)
        {
            var baseline = new Baseline { MaxEntries = 1000 };
            if (withPreviousCollection) baseline.LastCollectionUtc = PreviousCollection;
            return baseline;
        }

        /// <summary>
        /// A baseline that has kept a complete record since <paramref name="since"/>, its most recent collection
        /// the previous one.
        /// </summary>
        private static Baseline NewContinuousBaseline(DateTime since)
        {
            var baseline = new Baseline { MaxEntries = 1000 };
            baseline.Apply(new QueryStatsResult { SnapshotDateUtc = since });
            baseline.LastCollectionUtc = PreviousCollection;
            return baseline;
        }

        /// <summary>Seed the baseline as if the given row had been seen at the previous collection.</summary>
        private static void Seed(Baseline baseline, string planHandle, long executions, long workerTime,
            DateTime? creationTime = null, int startOffset = 0, int endOffset = 100, long planGeneration = 1,
            DateTime? lastSeen = null, long? maxElapsed = null)
        {
            var key = BaselineKey.Create(Convert.FromHexString(planHandle), startOffset, endOffset);
            baseline.Upsert(key, new BaselineEntry
            {
                ExecutionCount = executions,
                WorkerTime = workerTime,
                ElapsedTime = workerTime * 2,
                LogicalReads = executions * 10,
                LogicalWrites = 0,
                PhysicalReads = 0,
                ClrTime = 0,
                Rows = executions,
                Dop = executions,
                GrantKb = 0,
                UsedGrantKb = 0,
                Spills = 0,
                MaxElapsedTime = maxElapsed ?? (executions > 0 ? workerTime / executions * 2 : 0),
                CreationTimeUtc = creationTime ?? PreviousCollection.AddHours(-3),
                PlanGenerationNum = planGeneration,
                LastSeenTicks = (lastSeen ?? PreviousCollection).Ticks
            });
        }

        /// <summary>Process a fixture the way the collector processes the source query: as a reader.</summary>
        private static QueryStatsResult Process(DataTable raw, Baseline baseline, QueryStatsLimits limits) =>
            QueryStatsProcessor.Process(raw.CreateDataReader(), baseline, limits, DefaultInterval, Stopwatch.GetTimestamp());

        private static long Sum(DataTable dt, string column) =>
            dt.Rows.Cast<DataRow>().Sum(r => r[column] == DBNull.Value ? 0 : Convert.ToInt64(r[column]));

        private static long CollectionValue(DataTable collection, string column) =>
            Convert.ToInt64(collection.Rows[0][column]);

        [TestMethod]
        public void SteadyAccumulationStoresTheDifference()
        {
            var raw = NewRawTable();
            AddRow(raw, "AA01", executions: 150, workerTime: 9000);
            var baseline = NewBaseline();
            Seed(baseline, "AA01", executions: 100, workerTime: 5000);

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(1, result.QueryStats.Rows.Count);
            Assert.AreEqual(50L, Sum(result.QueryStats, "execution_count"), "only the executions since the previous collection belong to this interval");
            Assert.AreEqual(4000L, Sum(result.QueryStats, "total_worker_time"));
            Assert.IsFalse((bool)result.QueryStats.Rows[0]["IsCompile"]);
        }

        [TestMethod]
        public void PlanCompiledInsideTheIntervalIsTakenInFull()
        {
            var raw = NewRawTable();
            // No baseline, but the plan compiled after the previous collection, so all of its counters
            // accumulated inside this interval.
            AddRow(raw, "BB01", executions: 20, workerTime: 800, creationTime: PreviousCollection.AddMinutes(2));

            var result = Process(raw, NewBaseline(), QueryStatsLimits.Default);

            Assert.AreEqual(1, result.QueryStats.Rows.Count);
            Assert.AreEqual(20L, Sum(result.QueryStats, "execution_count"));
            Assert.AreEqual(800L, Sum(result.QueryStats, "total_worker_time"));
            Assert.IsTrue((bool)result.QueryStats.Rows[0]["IsCompile"], "a first sighting compiled inside the interval is a compile");
            Assert.AreEqual(0L, CollectionValue(result.Collection, "RowsUnattributed"));
        }

        [TestMethod]
        public void FirstSightingOfAnOlderPlanIsEstimatedNotAttributed()
        {
            var raw = NewRawTable();
            // No baseline and compiled two hours before the interval: how much of this happened in it is unknowable.
            AddRow(raw, "CC01", executions: 5000, workerTime: 250000, creationTime: PreviousCollection.AddHours(-2));

            var result = Process(raw, NewBaseline(), QueryStatsLimits.Default);

            Assert.AreEqual(0, result.QueryStats.Rows.Count, "unattributable work must not be presented as this interval's");
            Assert.AreEqual(1L, CollectionValue(result.Collection, "RowsUnattributed"), "but it must be counted");
            // The last execution in full, plus the other 4,999 prorated over the 125 minutes since the plan compiled,
            // five of which are this interval: not the plan's whole life, which would dwarf the interval's work.
            Assert.AreEqual(50L + 9998L, CollectionValue(result.Collection, "UnattributedWorkerTime"));
            Assert.AreEqual(1L + 200L, CollectionValue(result.Collection, "UnattributedExecutions"));
        }

        /// <summary>
        /// The other shape of first sighting: a job that runs once a day, whose plan has been cached for two weeks.
        /// Its last run is this interval's, and the earlier ones share almost nothing - which the proration gets
        /// right where a lifetime total would count fourteen runs into five minutes.
        /// </summary>
        [TestMethod]
        public void FirstSightingOfADailyJobCountsItsLastRun()
        {
            var raw = NewRawTable();
            AddRow(raw, "CD01", executions: 14, workerTime: 14 * 60000000L, creationTime: PreviousCollection.AddDays(-14));

            var result = Process(raw, NewBaseline(), QueryStatsLimits.Default);

            Assert.AreEqual(1L, CollectionValue(result.Collection, "UnattributedExecutions"));
            var worker = CollectionValue(result.Collection, "UnattributedWorkerTime");
            Assert.IsTrue(worker >= 60000000L && worker < 61000000L, $"one run's minute of CPU, give or take, not fourteen: {worker}");
        }

        [TestMethod]
        public void CountersGoingBackwardsAreTreatedAsAReset()
        {
            var raw = NewRawTable();
            // Same compile time and generation number, but fewer executions than last time: the row was
            // reset underneath us, so the difference is not a delta.
            AddRow(raw, "DD01", executions: 10, workerTime: 400, creationTime: PreviousCollection.AddHours(-2));
            var baseline = NewBaseline();
            Seed(baseline, "DD01", executions: 100, workerTime: 5000, creationTime: PreviousCollection.AddHours(-2));

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(0, result.QueryStats.Rows.Count);
            Assert.AreEqual(1L, CollectionValue(result.Collection, "RowsUnattributed"),
                "a reset whose compile time predates the interval cannot be attributed to it");
        }

        [TestMethod]
        public void RecompileIsNotTreatedAsAccumulation()
        {
            var raw = NewRawTable();
            AddRow(raw, "EE01", executions: 30, workerTime: 1500, creationTime: PreviousCollection.AddMinutes(1), planGeneration: 2);
            var baseline = NewBaseline();
            Seed(baseline, "EE01", executions: 100, workerTime: 5000, creationTime: PreviousCollection.AddHours(-2));

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(1, result.QueryStats.Rows.Count);
            Assert.AreEqual(30L, Sum(result.QueryStats, "execution_count"), "the recompiled row starts from zero, it is not 30 minus 100");
            Assert.IsTrue((bool)result.QueryStats.Rows[0]["IsCompile"]);
        }

        /// <summary>
        /// The claim the whole design rests on: truncating the breakdown must not change the total.
        /// </summary>
        [TestMethod]
        public void TotalsSurviveRankingAndRollup()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();
            var expectedExecutions = 0L;
            var expectedWorkerTime = 0L;

            // 200 distinct families, far more than the top N will keep.
            for (var i = 0; i < 200; i++)
            {
                var handle = $"F{i:X3}";
                var hash = $"{i + 1:X16}";
                Seed(baseline, handle, executions: 100, workerTime: 1000);
                // Distinct sql handles as well as distinct plan handles: without them these would be one
                // statement running under 200 plans rather than 200 separate queries.
                AddRow(raw, handle, executions: 100 + i, workerTime: 1000 + (i * 10), queryHash: hash,
                    sqlHandle: $"0200{i:X4}");
                expectedExecutions += i;
                expectedWorkerTime += i * 10;
            }

            var limits = new QueryStatsLimits { TopN = 10, MaxStatementsPerFamily = 3, MaxPlansPerStatement = 5 };
            var result = Process(raw, baseline, limits);

            Assert.IsTrue(result.QueryStats.Rows.Count < 200, "ranking should have truncated the breakdown");
            Assert.AreEqual(expectedExecutions, Sum(result.QueryStats, "execution_count"),
                "detail plus rollups must equal the interval's executions");
            Assert.AreEqual(expectedWorkerTime, Sum(result.QueryStats, "total_worker_time"),
                "detail plus rollups must equal the interval's worker time");
            Assert.AreEqual(0L, CollectionValue(result.Collection, "RowsUnattributed"));
        }

        /// <summary>
        /// An application that sends its values as literals: ten texts, one query.  They are one statement, the
        /// shape, rather than ten - and the plan's cache entries say how many texts are behind it.
        /// </summary>
        [TestMethod]
        public void LiteralVariantsAreOneStatement()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();
            var expectedExecutions = 0L;

            // Distinct handles, offsets and plan handles, one shared query hash and plan shape
            for (var i = 0; i < 10; i++)
            {
                var handle = $"A{i:X3}";
                Seed(baseline, handle, executions: 10, workerTime: 100, startOffset: i * 10, endOffset: (i * 10) + 8);
                AddRow(raw, handle, executions: 10 + i + 1, workerTime: 100 + (i * 10), startOffset: i * 10,
                    endOffset: (i * 10) + 8, sqlHandle: $"020000{i:X2}");
                expectedExecutions += i + 1;
            }

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(1, result.QueryStats.Rows.Count, "one statement under one plan, not ten statements");
            var row = result.QueryStats.Rows[0];
            Assert.AreEqual((byte)QueryStatsTables.StatementTypes.AdHocShape, (byte)row["StatementType"]);
            Assert.AreEqual(10, Convert.ToInt32(row["PlanCount"]), "the ten texts are ten cache entries behind the plan");
            Assert.AreEqual(expectedExecutions, Sum(result.QueryStats, "execution_count"));
            Assert.AreEqual("02000009", ((byte[])row["sql_handle"]).ToHexString(),
                "the example is the variant that did the most work in the interval");
            Assert.AreEqual(90, Convert.ToInt32(row["statement_start_offset"]), "with its own offsets");
        }

        /// <summary>
        /// A family spreads across statements when the same query sits in several procedures, and what the cap on
        /// them leaves out is rolled up rather than dropped.
        /// </summary>
        [TestMethod]
        public void StatementsOfOneFamilyRollUpWithoutLosingTheTotal()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();
            var expectedExecutions = 0L;

            // One query in ten procedures: distinct objects, one shared query hash
            for (var i = 0; i < 10; i++)
            {
                var handle = $"A{i:X3}";
                Seed(baseline, handle, executions: 10, workerTime: 100);
                AddRow(raw, handle, executions: 10 + i + 1, workerTime: 100 + i, objectName: $"Proc{i}",
                    sqlHandle: $"030000{i:X2}");
                expectedExecutions += i + 1;
            }

            var limits = new QueryStatsLimits { TopN = 50, MaxStatementsPerFamily = 3, MaxPlansPerStatement = 5 };
            var result = Process(raw, baseline, limits);

            var rollups = result.QueryStats.Rows.Cast<DataRow>()
                .Count(r => (byte)r["StatementType"] == (byte)QueryStatsTables.StatementTypes.OtherVariants);

            Assert.AreEqual(1, rollups, "the statements past the cap become exactly one row");
            Assert.AreEqual(4, result.QueryStats.Rows.Count, "three statements kept plus one rollup");
            Assert.AreEqual(expectedExecutions, Sum(result.QueryStats, "execution_count"));
        }

        /// <summary>
        /// The shapes go to the collector heaviest first, so a capped collection templates the ones that matter, each
        /// with the variant that did the most work and the QUOTED_IDENTIFIER setting its plan compiled under - which
        /// decides whether a double quoted token in its text is an identifier or a value.
        /// </summary>
        [TestMethod]
        public void ShapesGoToTheCollectorHeaviestFirstWithTheirExample()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();
            Seed(baseline, "AB01", executions: 10, workerTime: 100, startOffset: 0, endOffset: 50);
            Seed(baseline, "AB02", executions: 10, workerTime: 100, startOffset: 100, endOffset: 150);
            Seed(baseline, "AB03", executions: 10, workerTime: 100);
            AddRow(raw, "AB01", executions: 20, workerTime: 300, sqlHandle: "020000A1", startOffset: 0, endOffset: 50);
            AddRow(raw, "AB02", executions: 20, workerTime: 2100, sqlHandle: "020000A2", startOffset: 100, endOffset: 150,
                setOptions: DefaultSetOptions & ~64);
            AddRow(raw, "AB03", executions: 20, workerTime: 600, sqlHandle: "020000B1", queryHash: "BBBBBBBBBBBBBBBB");

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(2, result.Shapes.Count);
            var heavy = result.Shapes[0];
            Assert.AreEqual("AAAAAAAAAAAAAAAA", heavy.QueryHash.ToHexString(), "the heavier shape first");
            Assert.AreEqual("020000A2", heavy.SqlHandle.ToHexString(), "its example is the variant that did the most work");
            Assert.AreEqual(100, heavy.StartOffset);
            Assert.AreEqual(150, heavy.EndOffset);
            Assert.IsFalse(heavy.QuotedIdentifier, "taken from the example's own plan");
            Assert.AreEqual(1, heavy.Rows.Count);
            Assert.AreEqual("020000A2", ((byte[])heavy.Rows[0]["sql_handle"]).ToHexString(), "and the rows carry the same example");
            Assert.IsTrue(result.Shapes[1].QuotedIdentifier);
            Assert.AreEqual(0, result.TextHandles.Count, "a shape's text goes with its template, not through the text collection");
        }

        /// <summary>
        /// Each plan shape kept goes to the collector with the cache entry that did the most work under it, whose plan
        /// stands for the row, and they go heaviest first by the row's work so a capped collection fetches the plans
        /// that matter.
        /// </summary>
        [TestMethod]
        public void PlansGoToTheCollectorHeaviestFirstWithTheEntryThatDidTheMostWork()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();
            // An ad hoc shape: three variants under one plan shape, the second doing the most work
            Seed(baseline, "AB01", executions: 10, workerTime: 100, startOffset: 0, endOffset: 50);
            Seed(baseline, "AB02", executions: 10, workerTime: 100, startOffset: 100, endOffset: 150);
            Seed(baseline, "AB03", executions: 10, workerTime: 100, startOffset: 0, endOffset: 50);
            AddRow(raw, "AB01", executions: 20, workerTime: 300, sqlHandle: "020000A1", startOffset: 0, endOffset: 50);
            AddRow(raw, "AB02", executions: 20, workerTime: 900, sqlHandle: "020000A2", startOffset: 100, endOffset: 150);
            AddRow(raw, "AB03", executions: 20, workerTime: 200, sqlHandle: "020000A3", startOffset: 0, endOffset: 50);
            // A procedure statement under two plan shapes, one of them heavier than the whole ad hoc shape
            Seed(baseline, "BA01", executions: 10, workerTime: 100);
            Seed(baseline, "BA02", executions: 10, workerTime: 100);
            AddRow(raw, "BA01", executions: 20, workerTime: 5000, objectName: "MyProc", queryHash: "CCCCCCCCCCCCCCCC",
                planHash: "2222222222222222");
            AddRow(raw, "BA02", executions: 20, workerTime: 150, objectName: "MyProc", queryHash: "CCCCCCCCCCCCCCCC",
                planHash: "3333333333333333");

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            CollectionAssert.AreEqual(new[] { "2222222222222222", "1111111111111111", "3333333333333333" },
                result.Plans.Select(p => p.PlanHash.ToHexString()).ToList(),
                "one per plan shape kept, heaviest first by the row's work");
            var shape = result.Plans[1];
            Assert.AreEqual("AB02", shape.PlanHandle.ToHexString(), "the entry that did the most work under the plan shape");
            Assert.AreEqual(100, shape.StartOffset, "with its own offsets");
            Assert.AreEqual(150, shape.EndOffset);
            Assert.AreEqual(1100L, shape.WorkerTime, "ordered by the row's work, every variant's together");
            Assert.AreSame(result.QueryStats.Rows.Cast<DataRow>()
                .Single(r => ((byte[])r["query_plan_hash"]).ToHexString() == "1111111111111111"), shape.Row,
                "and goes with the row it is the plan for");
            Assert.AreEqual("BA01", result.Plans[0].PlanHandle.ToHexString());
            Assert.AreEqual("BA02", result.Plans[2].PlanHandle.ToHexString(), "each plan shape with an entry of its own");
        }

        /// <summary>
        /// A row with no plan of its own gets none: the plans past the per statement cap are one row standing for
        /// several, and a statement whose plan hash is zeros has nothing to fetch.
        /// </summary>
        [TestMethod]
        public void OnlyRowsWithAPlanOfTheirOwnGetOne()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();
            Seed(baseline, "CA01", executions: 10, workerTime: 100);
            Seed(baseline, "CA02", executions: 10, workerTime: 100);
            Seed(baseline, "CA03", executions: 10, workerTime: 100);
            AddRow(raw, "CA01", executions: 20, workerTime: 900, objectName: "MyProc", planHash: "1111111111111111");
            AddRow(raw, "CA02", executions: 20, workerTime: 600, objectName: "MyProc", planHash: "2222222222222222");
            AddRow(raw, "CA03", executions: 20, workerTime: 300, objectName: "MyProc", planHash: "3333333333333333");
            Seed(baseline, "CB01", executions: 10, workerTime: 100);
            AddRow(raw, "CB01", executions: 20, workerTime: 800, queryHash: "BBBBBBBBBBBBBBBB", planHash: "0000000000000000",
                sqlHandle: "020000B1");

            var limits = new QueryStatsLimits { TopN = 50, MaxStatementsPerFamily = 3, MaxPlansPerStatement = 2 };
            var result = Process(raw, baseline, limits);

            CollectionAssert.AreEquivalent(new[] { "1111111111111111", "2222222222222222" },
                result.Plans.Select(p => p.PlanHash.ToHexString()).ToList(),
                "not the plan past the cap, rolled up into a row with no plan, nor a hash of zeros");
            Assert.AreEqual(1, result.QueryStats.Rows.Cast<DataRow>().Count(r => (bool)r["IsOtherPlans"]),
                "the plan past the cap is still counted");
            Assert.IsTrue(result.QueryStats.Rows.Cast<DataRow>().Any(r => (byte)r["StatementType"] == (byte)QueryStatsTables.StatementTypes.AdHocShape),
                "and so is the statement with no plan hash");
        }

        /// <summary>
        /// The collector remembers a plan as sent by the statement and plan shape, because the entry that stands for
        /// an ad hoc shape is a different variant from one interval to the next - remembered by entry, every shape's
        /// plan would be fetched again every interval.
        /// </summary>
        [TestMethod]
        public void APlanIsRememberedByItsStatementNotByTheEntryThatStoodForIt()
        {
            QueryStatsResult Interval(long firstWork, long secondWork)
            {
                var raw = NewRawTable();
                var baseline = NewBaseline();
                Seed(baseline, "AB01", executions: 10, workerTime: 100, startOffset: 0, endOffset: 50);
                Seed(baseline, "AB02", executions: 10, workerTime: 100, startOffset: 100, endOffset: 150);
                AddRow(raw, "AB01", executions: 20, workerTime: firstWork, sqlHandle: "020000A1", startOffset: 0, endOffset: 50);
                AddRow(raw, "AB02", executions: 20, workerTime: secondWork, sqlHandle: "020000A2", startOffset: 100, endOffset: 150);
                return Process(raw, baseline, QueryStatsLimits.Default);
            }

            var first = Interval(900, 300).Plans.Single();
            var second = Interval(300, 900).Plans.Single();

            Assert.AreEqual("AB01", first.PlanHandle.ToHexString());
            Assert.AreEqual("AB02", second.PlanHandle.ToHexString(), "a different variant did the most work");
            Assert.AreEqual(first.StatementKey, second.StatementKey, "but it is the same statement");

            var raw = NewRawTable();
            var baseline = NewBaseline();
            Seed(baseline, "AB01", executions: 10, workerTime: 100);
            Seed(baseline, "AB02", executions: 10, workerTime: 100);
            AddRow(raw, "AB01", executions: 20, workerTime: 300, database: "AppDB");
            AddRow(raw, "AB02", executions: 20, workerTime: 300, database: "OtherDB");
            var databases = Process(raw, baseline, QueryStatsLimits.Default).Plans;
            Assert.AreEqual(2, databases.Select(p => p.StatementKey).Distinct().Count(),
                "the same text in two databases is two statements, each with its own plans");
        }

        [TestMethod]
        public void SameTextInTwoDatabasesStaysTwoStatements()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();
            // The same ad hoc text executed in two databases shares one sql_handle but has two plan handles.
            Seed(baseline, "AB01", executions: 10, workerTime: 100);
            Seed(baseline, "AB02", executions: 10, workerTime: 100);
            AddRow(raw, "AB01", executions: 20, workerTime: 300, database: "AppDB");
            AddRow(raw, "AB02", executions: 15, workerTime: 250, database: "OtherDB");

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(2, result.QueryStats.Rows.Count);
            var databases = result.QueryStats.Rows.Cast<DataRow>().Select(r => (string)r["database_name"]).OrderBy(d => d).ToList();
            CollectionAssert.AreEqual(new[] { "AppDB", "OtherDB" }, databases);
            Assert.AreEqual(15L, Sum(result.QueryStats, "execution_count"), "10 from one database and 5 from the other");
        }

        [TestMethod]
        public void OnePlanPerStatementIsTheNormalCaseAndTwoIsVisible()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();
            // One statement, two plan shapes: the plan regression case, which must stay two rows.
            Seed(baseline, "BA01", executions: 10, workerTime: 100);
            Seed(baseline, "BA02", executions: 10, workerTime: 100);
            AddRow(raw, "BA01", executions: 20, workerTime: 200, objectName: "MyProc", planHash: "1111111111111111");
            AddRow(raw, "BA02", executions: 12, workerTime: 900, objectName: "MyProc", planHash: "2222222222222222");

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(2, result.QueryStats.Rows.Count, "a statement that ran under two plan shapes is two rows");
            Assert.AreEqual(12L, Sum(result.QueryStats, "execution_count"));
            Assert.AreEqual(1, result.QueryStats.Rows.Cast<DataRow>()
                .Select(r => ((byte[])r["sql_handle"]).ToHexString()).Distinct().Count(), "both rows are the same statement");
        }

        [TestMethod]
        public void OneExpensiveExecutionKeepsItsFamily()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();

            // Enough noisy families to fill the ranking several times over.
            for (var i = 0; i < 50; i++)
            {
                var handle = $"B{i:X3}";
                Seed(baseline, handle, executions: 1000, workerTime: 100000);
                AddRow(raw, handle, executions: 2000, workerTime: 200000, queryHash: $"{i + 1:X16}",
                    sqlHandle: $"0201{i:X4}");
            }

            // One execution, nowhere near the top of any total, but slow enough on its own to matter.  The
            // noisy families each run 1000 times for 200 microseconds elapsed apiece, so the threshold below
            // separates them cleanly: this is the one that would be lost.
            Seed(baseline, "FADE", executions: 0, workerTime: 0);
            AddRow(raw, "FADE", executions: 1, workerTime: 1000, queryHash: "FFFFFFFFFFFFFFFF",
                planHash: "9999999999999999", sqlHandle: "02FFFFFF");

            var limits = new QueryStatsLimits
            {
                TopN = 5,
                MaxStatementsPerFamily = 3,
                MaxPlansPerStatement = 5,
                // Elapsed time is twice worker time in this fixture: 2000 microseconds for the rare row's
                // single execution, against 200 for each of the noisy families'.
                SingleExecutionElapsedThreshold = 2000
            };
            var result = Process(raw, baseline, limits);

            var rare = result.QueryStats.Rows.Cast<DataRow>()
                .Any(r => r["query_hash"] != DBNull.Value && ((byte[])r["query_hash"]).ToHexString() == "FFFFFFFFFFFFFFFF"
                          && (byte)r["StatementType"] != (byte)QueryStatsTables.StatementTypes.OtherQueries);

            var diagnostic = string.Join(", ", result.QueryStats.Rows.Cast<DataRow>()
                .Select(r => $"[type={r["StatementType"]} hash={(r["query_hash"] == DBNull.Value ? "null" : ((byte[])r["query_hash"]).ToHexString())} execs={r["execution_count"]}]")
                .Take(60));
            Assert.IsTrue(rare, "a single expensive execution is exactly what a top N by total would hide. Rows: " + diagnostic);
        }

        [TestMethod]
        public void LargeMemoryGrantKeepsItsFamily()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();

            // Noisy families that lead every other measure and need no grant at all.
            for (var i = 0; i < 50; i++)
            {
                var handle = $"C{i:X3}";
                Seed(baseline, handle, executions: 1000, workerTime: 100000);
                AddRow(raw, handle, executions: 2000, workerTime: 200000, queryHash: $"{i + 1:X16}",
                    sqlHandle: $"0202{i:X4}");
            }

            // Two executions, cheap on CPU and reads, but each reserving 1 GB.
            Seed(baseline, "BEEF", executions: 0, workerTime: 0);
            var hungry = AddRow(raw, "BEEF", executions: 2, workerTime: 20, queryHash: "EEEEEEEEEEEEEEEE",
                planHash: "8888888888888888", sqlHandle: "02EEEEEE");
            hungry["total_grant_kb"] = 2L * 1024 * 1024;
            hungry["total_used_grant_kb"] = 1024L;

            var limits = new QueryStatsLimits
            {
                TopN = 5,
                MaxStatementsPerFamily = 3,
                MaxPlansPerStatement = 5,
                SingleExecutionElapsedThreshold = long.MaxValue
            };
            var result = Process(raw, baseline, limits);

            var kept = result.QueryStats.Rows.Cast<DataRow>()
                .SingleOrDefault(r => r["query_hash"] != DBNull.Value && ((byte[])r["query_hash"]).ToHexString() == "EEEEEEEEEEEEEEEE"
                                      && (byte)r["StatementType"] != (byte)QueryStatsTables.StatementTypes.OtherQueries);

            Assert.IsNotNull(kept, "a memory hungry query that is cheap on every other measure must not be rolled up");
            Assert.AreEqual(2L * 1024 * 1024, Convert.ToInt64(kept["total_grant_kb"]));
        }

        /// <summary>
        /// Text is fetched for the statements that were stored, heaviest first, because a capped collection
        /// has to spend its budget on the rows someone will actually click on.  Not for an ad hoc shape, whose
        /// example is a different variant from one interval to the next: that goes to the collector with the
        /// shape, to have its template made once.
        /// </summary>
        [TestMethod]
        public void TextHandlesCoverTheStoredStatementsHeaviestFirst()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();

            // Offsets are part of the baseline key, so they have to match the rows below
            Seed(baseline, "AA01", executions: 10, workerTime: 100, startOffset: 0, endOffset: 10);
            Seed(baseline, "AA02", executions: 10, workerTime: 100, startOffset: 20, endOffset: 30);
            Seed(baseline, "AA03", executions: 10, workerTime: 100, startOffset: 40, endOffset: 50);
            Seed(baseline, "AA04", executions: 10, workerTime: 100, startOffset: 60, endOffset: 70);
            // One query in two procedures, the cheaper of which the per-family cap rolls up, a procedure
            // statement of its own, and an ad hoc shape.
            AddRow(raw, "AA01", executions: 20, workerTime: 5000, objectName: "ProcA", sqlHandle: "030000AA",
                startOffset: 0, endOffset: 10);
            AddRow(raw, "AA02", executions: 20, workerTime: 200, objectName: "ProcB", sqlHandle: "030000BB",
                startOffset: 20, endOffset: 30);
            AddRow(raw, "AA03", executions: 20, workerTime: 900, objectName: "ProcC", sqlHandle: "030000CC",
                startOffset: 40, endOffset: 50, queryHash: "DDDDDDDDDDDDDDDD");
            AddRow(raw, "AA04", executions: 20, workerTime: 3000, sqlHandle: "020000EE", startOffset: 60, endOffset: 70,
                queryHash: "EEEEEEEEEEEEEEEE");

            var limits = new QueryStatsLimits { TopN = 50, MaxStatementsPerFamily = 1, MaxPlansPerStatement = 5 };
            var result = Process(raw, baseline, limits);

            var handles = result.TextHandles.Select(h => h.ToHexString()).ToList();

            CollectionAssert.AreEqual(new[] { "030000AA", "030000CC" }, handles,
                "the two procedure statements that were stored, heaviest first, and not the one that was rolled up");
            Assert.AreEqual("020000EE", result.Shapes.Single().SqlHandle.ToHexString(), "the shape goes with its example instead");
        }

        [TestMethod]
        public void ProcessingDoesNotMoveTheBaseline()
        {
            var raw = NewRawTable();
            AddRow(raw, "CA01", executions: 50, workerTime: 2000, creationTime: PreviousCollection.AddMinutes(1));
            var baseline = NewBaseline();

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(0, baseline.Count, "processing must not move the baseline");
            Assert.AreEqual(1, result.PendingBaseline.Count, "it is returned for Apply to move it");
            Assert.AreEqual(PreviousCollection, baseline.LastCollectionUtc);
        }

        [TestMethod]
        public void ApplyMovesTheBaselineOnAndStartsItsRecord()
        {
            var baseline = new Baseline { MaxEntries = 1000 };
            var raw = NewRawTable();
            AddRow(raw, "CB01", executions: 50, workerTime: 2000, creationTime: Snapshot.AddMinutes(-1));
            var result = Process(raw, baseline, QueryStatsLimits.Default);
            result.ReadDurationMs = 25;

            baseline.Apply(result);

            Assert.AreEqual(Snapshot, baseline.LastCollectionUtc, "the next interval starts at this collection's snapshot");
            Assert.AreEqual(Snapshot, baseline.ContinuousSinceUtc, "an empty baseline starts its complete record here");
            Assert.AreEqual(25, baseline.LastReadDurationMs, "and the read duration that decides whether the next read is skipped");
            Assert.AreEqual(1, baseline.Count);
            Assert.IsFalse(baseline.IsFirstCollection);

            var later = new QueryStatsResult { SnapshotDateUtc = Snapshot.AddMinutes(5) };
            baseline.Apply(later);
            Assert.AreEqual(Snapshot, baseline.ContinuousSinceUtc, "and keeps it while it stays unbroken");
        }

        /// <summary>
        /// The guarantee that moving the baseline before the write rests on: each collection covers only what
        /// happened after the one before, so no interval is ever reported twice, whatever became of the previous
        /// collection's write.
        /// </summary>
        [TestMethod]
        public void AppliedCollectionsMeetEndToStart()
        {
            var baseline = NewBaseline();
            Seed(baseline, "CC01", executions: 100, workerTime: 5000);

            var first = NewRawTable();
            AddRow(first, "CC01", executions: 150, workerTime: 9000);
            baseline.Apply(Process(first, baseline, QueryStatsLimits.Default));

            var second = NewRawTable();
            var row = AddRow(second, "CC01", executions: 170, workerTime: 9500);
            row["SnapshotDateUTC"] = Snapshot.AddMinutes(5);
            var result = Process(second, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(20L, Sum(result.QueryStats, "execution_count"), "only what ran after the first collection");
            Assert.AreEqual(500L, Sum(result.QueryStats, "total_worker_time"));
            Assert.AreEqual(5 * 60 * 1000000L, Convert.ToInt64(result.QueryStats.Rows[0]["PeriodTime"]),
                "and the period starts where the first one ended");
        }

        /// <summary>
        /// Two collections for one connection at once would diff against the same baseline and report the same
        /// interval twice, so the second waits.  Other connections don't.
        /// </summary>
        [TestMethod]
        public async Task OneCollectionPerConnectionAtATime()
        {
            var held = await QueryStatsBaselineStore.LockAsync("QueryStatsLockTest-A");
            // Matched without regard to case, as the baselines themselves are
            var sameConnection = QueryStatsBaselineStore.LockAsync("querystatslocktest-a").AsTask();
            var otherConnection = QueryStatsBaselineStore.LockAsync("QueryStatsLockTest-B").AsTask();

            (await otherConnection.WaitAsync(TimeSpan.FromSeconds(10))).Dispose();
            Assert.IsFalse(sameConnection.IsCompleted, "a second collection for the connection waits for the first");

            held.Dispose();
            (await sameConnection.WaitAsync(TimeSpan.FromSeconds(10))).Dispose();
        }

        /// <summary>
        /// The long execution: started before the previous collection and finished inside this one.  Its plan
        /// compiled before the interval, so the compile rule can't place it - but the baseline has kept a complete
        /// record since before the plan existed and never saw it finish, so everything on the row was done in
        /// this interval.
        /// </summary>
        [TestMethod]
        public void LongExecutionFinishingInsideTheIntervalIsAttributed()
        {
            var raw = NewRawTable();
            // Started two minutes before the previous collection and ran for four
            AddRow(raw, "1A01", executions: 1, workerTime: 120000000, creationTime: PreviousCollection.AddMinutes(-2));
            var baseline = NewContinuousBaseline(PreviousCollection.AddHours(-1));

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(1, result.QueryStats.Rows.Count);
            Assert.AreEqual(120000000L, Sum(result.QueryStats, "total_worker_time"));
            Assert.IsFalse((bool)result.QueryStats.Rows[0]["IsCompile"], "it compiled before the interval, so it is not a compile");
            Assert.AreEqual(0L, CollectionValue(result.Collection, "RowsUnattributed"));
        }

        /// <summary>A baseline that started after the plan compiled can't vouch for what the plan did before it.</summary>
        [TestMethod]
        public void PlanOlderThanTheBaselineRecordIsNotTakenWhole()
        {
            var raw = NewRawTable();
            AddRow(raw, "1B01", executions: 1, workerTime: 120000000, creationTime: PreviousCollection.AddHours(-2));
            var baseline = NewContinuousBaseline(PreviousCollection.AddHours(-1));

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(0, result.QueryStats.Rows.Count);
            Assert.AreEqual(1L, CollectionValue(result.Collection, "RowsUnattributed"));
        }

        /// <summary>
        /// A trimmed entry looks exactly like a row that was never seen.  A row compiled before the most recent
        /// trim could be one of those it dropped, so only a row compiled after it is taken whole.
        /// </summary>
        [TestMethod]
        public void PlanThatMightHaveBeenTrimmedIsNotTakenWhole()
        {
            var since = PreviousCollection.AddHours(-1);
            var baseline = NewContinuousBaseline(since);
            Seed(baseline, "1C01", executions: 10, workerTime: 100, lastSeen: since.AddMinutes(10));
            Seed(baseline, "1C02", executions: 10, workerTime: 100, lastSeen: since.AddMinutes(20));
            baseline.MaxEntries = 1;
            baseline.Trim();
            Assert.AreEqual(since.AddMinutes(10), baseline.EvictedThroughUtc, "the older entry was trimmed");

            var raw = NewRawTable();
            AddRow(raw, "1C03", executions: 1, workerTime: 1000, creationTime: since.AddMinutes(5), sqlHandle: "0200000031");
            AddRow(raw, "1C04", executions: 1, workerTime: 2000, creationTime: since.AddMinutes(15), sqlHandle: "0200000032");

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(1L, CollectionValue(result.Collection, "RowsUnattributed"), "compiled before the trim");
            Assert.AreEqual(1000L, CollectionValue(result.Collection, "UnattributedWorkerTime"));
            Assert.AreEqual(2000L, Sum(result.QueryStats, "total_worker_time"), "compiled after it");
        }

        /// <summary>
        /// Recompiled before the interval and not seen since: the entry under the key belongs to the plan the
        /// recompile replaced, and the new one finished nothing before the previous collection.
        /// </summary>
        [TestMethod]
        public void RecompiledBeforeTheIntervalAndFirstFinishedInsideIsAttributed()
        {
            var baseline = NewContinuousBaseline(PreviousCollection.AddHours(-1));
            Seed(baseline, "1D01", executions: 100, workerTime: 5000, creationTime: PreviousCollection.AddHours(-2));
            var raw = NewRawTable();
            AddRow(raw, "1D01", executions: 3, workerTime: 900, creationTime: PreviousCollection.AddMinutes(-3), planGeneration: 2);

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(3L, Sum(result.QueryStats, "execution_count"));
            Assert.AreEqual(0L, CollectionValue(result.Collection, "RowsUnattributed"));
        }

        /// <summary>
        /// The generation number moved but the compile time didn't, so the row was seen at this compile time and
        /// its counters may still include work the previous delta took.  Not taken whole.
        /// </summary>
        [TestMethod]
        public void GenerationChangeAtTheSameCompileTimeIsNotTakenWhole()
        {
            var baseline = NewContinuousBaseline(PreviousCollection.AddHours(-1));
            var compiled = PreviousCollection.AddMinutes(-30);
            Seed(baseline, "1E01", executions: 100, workerTime: 5000, creationTime: compiled);
            var raw = NewRawTable();
            AddRow(raw, "1E01", executions: 110, workerTime: 5500, creationTime: compiled, planGeneration: 2);

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(0, result.QueryStats.Rows.Count);
            Assert.AreEqual(1L, CollectionValue(result.Collection, "RowsUnattributed"));
        }

        [TestMethod]
        public void TheRecordSurvivesARestartAndEndsWithADiscard()
        {
            var baseline = NewContinuousBaseline(PreviousCollection.AddHours(-1));
            Seed(baseline, "1F01", executions: 10, workerTime: 100, lastSeen: PreviousCollection.AddMinutes(-50));
            Seed(baseline, "1F02", executions: 10, workerTime: 100);
            baseline.MaxEntries = 1;
            baseline.Trim();

            var path = Path.Combine(Path.GetTempPath(), $"QueryStatsBaselineTest_{Guid.NewGuid():N}.bin");
            try
            {
                baseline.Save(path);
                var loaded = Baseline.Load(path, 1000);

                Assert.AreEqual(baseline.LastCollectionUtc, loaded.LastCollectionUtc);
                Assert.AreEqual(baseline.ContinuousSinceUtc, loaded.ContinuousSinceUtc);
                Assert.AreEqual(baseline.EvictedThroughUtc, loaded.EvictedThroughUtc);
                Assert.AreEqual(1, loaded.Count);

                loaded.DiscardIfStale(Snapshot.AddDays(3), TimeSpan.FromMinutes(60));
                Assert.IsNull(loaded.ContinuousSinceUtc, "a discarded baseline has no record to vouch for");
                Assert.IsNull(loaded.EvictedThroughUtc);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void MeasuresTheInstanceCannotSupplyStayNull()
        {
            var raw = NewRawTable();
            var row = AddRow(raw, "DA01", executions: 20, workerTime: 400, creationTime: PreviousCollection.AddMinutes(1));
            // A pre-2016 instance has no spills or grant columns to report.
            row["total_spills"] = DBNull.Value;
            row["total_grant_kb"] = DBNull.Value;

            var result = Process(raw, NewBaseline(), QueryStatsLimits.Default);

            Assert.AreEqual(DBNull.Value, result.QueryStats.Rows[0]["total_spills"], "null is not the same as no spills");
            Assert.AreEqual(DBNull.Value, result.QueryStats.Rows[0]["total_grant_kb"]);
            Assert.AreEqual(20L, Convert.ToInt64(result.QueryStats.Rows[0]["total_rows"]), "supported measures are unaffected");
        }

        /// <summary>
        /// A gap of a few minutes is still diffed: a restart or a couple of missed intervals should produce a
        /// real delta rather than a hole.
        /// </summary>
        [TestMethod]
        public void ShortGapKeepsTheBaseline()
        {
            var baseline = NewBaseline();
            baseline.LastCollectionUtc = Snapshot.AddMinutes(-12);
            Seed(baseline, "FA01", executions: 100, workerTime: 5000);

            var discarded = baseline.DiscardIfStale(Snapshot, TimeSpan.FromMinutes(60));

            Assert.IsFalse(discarded);
            Assert.AreEqual(1, baseline.Count);

            var raw = NewRawTable();
            AddRow(raw, "FA01", executions: 150, workerTime: 9000);
            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(50L, Sum(result.QueryStats, "execution_count"), "the delta across the gap is real work");
            Assert.AreEqual(12 * 60 * 1000000L, Convert.ToInt64(result.QueryStats.Rows[0]["PeriodTime"]));
        }

        /// <summary>
        /// The case that matters: a service stopped for days.  Diffing against a baseline that old would
        /// report days of work as one interval, which any report window containing that snapshot would then
        /// attribute in full.  The baseline is thrown away instead and the work counted as unattributed.
        /// </summary>
        [TestMethod]
        public void LongGapDiscardsTheBaselineRatherThanReportingASpike()
        {
            var baseline = NewBaseline();
            baseline.LastCollectionUtc = Snapshot.AddDays(-3);
            Seed(baseline, "FB01", executions: 100, workerTime: 5000, creationTime: Snapshot.AddDays(-4));

            var discarded = baseline.DiscardIfStale(Snapshot, TimeSpan.FromMinutes(60));

            Assert.IsTrue(discarded);
            Assert.AreEqual(0, baseline.Count, "the stale entries are gone");
            Assert.IsTrue(baseline.IsFirstCollection, "so the collection reports itself as having no baseline");

            var raw = NewRawTable();
            // Three days of accumulation on a plan that was compiled before the outage started.
            AddRow(raw, "FB01", executions: 900000, workerTime: 45000000, creationTime: Snapshot.AddDays(-4));

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(0, result.QueryStats.Rows.Count,
                "three days of work must not be presented as this interval's");
            // The last execution plus the rest prorated over the four days since the plan compiled
            Assert.AreEqual(1L + 781L, CollectionValue(result.Collection, "UnattributedExecutions"),
                "it is counted rather than dropped, as the interval's estimated share rather than four days in five minutes");
            Assert.AreEqual(1L, CollectionValue(result.Collection, "IsFirstCollection"));
        }

        /// <summary>
        /// After a discarded baseline, a plan compiled inside the short window that replaces it is still
        /// attributed - that work demonstrably happened in the interval.
        /// </summary>
        [TestMethod]
        public void AfterALongGapWhatCompiledInsideTheWindowIsStillCounted()
        {
            var baseline = NewBaseline();
            baseline.LastCollectionUtc = Snapshot.AddDays(-3);
            baseline.DiscardIfStale(Snapshot, TimeSpan.FromMinutes(60));

            var raw = NewRawTable();
            AddRow(raw, "FC01", executions: 40, workerTime: 1600, creationTime: Snapshot.AddMinutes(-2));

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(1, result.QueryStats.Rows.Count);
            Assert.AreEqual(40L, Sum(result.QueryStats, "execution_count"));
            Assert.IsTrue((bool)result.QueryStats.Rows[0]["IsCompile"]);
            Assert.AreEqual(5 * 60 * 1000000L, Convert.ToInt64(result.QueryStats.Rows[0]["PeriodTime"]),
                "and the period is the short default window, not the outage");
        }

        [TestMethod]
        public void PeriodTimeCoversAMissedCollection()
        {
            var raw = NewRawTable();
            AddRow(raw, "EA01", executions: 50, workerTime: 2000);
            var baseline = NewBaseline();
            // The previous successful collection was 20 minutes ago, not 5: three intervals were missed.
            baseline.LastCollectionUtc = Snapshot.AddMinutes(-20);
            Seed(baseline, "EA01", executions: 10, workerTime: 500);

            var result = Process(raw, baseline, QueryStatsLimits.Default);

            Assert.AreEqual(20 * 60 * 1000000L, Convert.ToInt64(result.QueryStats.Rows[0]["PeriodTime"]),
                "the delta covers the whole gap, and the row says so rather than implying five minutes");
        }

        /// <summary>
        /// One busy family the ranking keeps on every measure, and one small family it drops: 1,000 executions in
        /// the interval, 999 at 200 microseconds and one at 4.8 seconds - an average of 5ms, far under the single
        /// execution threshold.  Returns whether the small family was kept as detail.
        /// </summary>
        private static bool SlowRunKeepsItsFamily(long lastElapsed, long previousMax, long currentMax)
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();

            AddRow(raw, "D101", executions: 5000, workerTime: 5000000, creationTime: PreviousCollection.AddMinutes(1),
                queryHash: "1111111111111111", sqlHandle: "0200000101");

            Seed(baseline, "D102", executions: 1000, workerTime: 100000, maxElapsed: previousMax);
            var row = AddRow(raw, "D102", executions: 2000, workerTime: 200000, queryHash: "2222222222222222",
                sqlHandle: "0200000102", lastElapsed: lastElapsed, maxElapsed: currentMax);
            row["total_elapsed_time"] = 200000L + 999 * 200L + 4800200L;

            var limits = new QueryStatsLimits
            {
                TopN = 1,
                MaxStatementsPerFamily = 3,
                MaxPlansPerStatement = 5,
                SingleExecutionElapsedThreshold = 1000000
            };
            var result = Process(raw, baseline, limits);

            return result.QueryStats.Rows.Cast<DataRow>().Any(r =>
                (byte)r["StatementType"] != (byte)QueryStatsTables.StatementTypes.OtherQueries
                && ((byte[])r["query_hash"]).ToHexString() == "2222222222222222");
        }

        [TestMethod]
        public void SlowLastExecutionKeepsItsFamily()
        {
            Assert.IsTrue(SlowRunKeepsItsFamily(lastElapsed: 4800200, previousMax: 9000000, currentMax: 9000000),
                "the last execution finished inside the interval, so its time is this interval's");
        }

        [TestMethod]
        public void NewMaximumKeepsItsFamily()
        {
            Assert.IsTrue(SlowRunKeepsItsFamily(lastElapsed: 200, previousMax: 300, currentMax: 4800200),
                "a maximum that rose since the previous collection was set inside the interval");
        }

        /// <summary>
        /// The limit of what the plan cache can show: the slow run was neither the last nor a new maximum, because
        /// a slower one came before the interval.  A lifetime maximum that didn't move says nothing about it.
        /// </summary>
        [TestMethod]
        public void SlowRunThatSetNoNewMaximumIsNotSeen()
        {
            Assert.IsFalse(SlowRunKeepsItsFamily(lastElapsed: 200, previousMax: 9000000, currentMax: 9000000),
                "a maximum from before the interval must not be taken for this interval's");
        }

        /// <summary>
        /// An instance with more databases than the top N writes a rollup row for the busiest of them and one for
        /// the rest together, rather than a row for every database - and the interval's total is unchanged.
        /// </summary>
        [TestMethod]
        public void DatabaseRollupsAreCappedWithoutLosingTheTotal()
        {
            var raw = NewRawTable();
            var baseline = NewBaseline();

            // One busy family that takes every ranking, and a small family in each of twenty databases that misses
            Seed(baseline, "E000", executions: 100, workerTime: 1000);
            AddRow(raw, "E000", executions: 10100, workerTime: 1001000, queryHash: "0000000000000001", sqlHandle: "0200009999");
            for (var i = 1; i <= 20; i++)
            {
                var handle = $"E{i:X3}";
                Seed(baseline, handle, executions: 100, workerTime: 1000);
                AddRow(raw, handle, executions: 100 + i, workerTime: 1000 + i * 10, database: $"DB{i:D2}",
                    queryHash: $"{i + 1:X16}", sqlHandle: $"0201{i:X4}");
            }

            var limits = new QueryStatsLimits { TopN = 1, MaxStatementsPerFamily = 3, MaxPlansPerStatement = 5 };
            var result = Process(raw, baseline, limits);

            var rows = result.QueryStats.Rows.Cast<DataRow>().ToList();
            var perDatabase = rows.Where(r => (byte)r["StatementType"] == (byte)QueryStatsTables.StatementTypes.OtherQueries).ToList();
            var otherDatabases = rows.Where(r => (byte)r["StatementType"] == (byte)QueryStatsTables.StatementTypes.OtherDatabases).ToList();

            Assert.AreEqual(1, perDatabase.Count, "a rollup of its own for the busiest database only");
            Assert.AreEqual("DB20", perDatabase[0]["database_name"]);
            Assert.AreEqual(1, otherDatabases.Count, "and one for the other nineteen together");
            Assert.AreEqual(DBNull.Value, otherDatabases[0]["database_name"]);
            Assert.AreEqual(10L * (19 * 20 / 2), Convert.ToInt64(otherDatabases[0]["total_worker_time"]),
                "DB01 to DB19's work");
            Assert.AreEqual(1000000L + 10L * (20 * 21 / 2), Sum(result.QueryStats, "total_worker_time"),
                "detail plus rollups still add up to the interval's work");
        }

        /// <summary>
        /// A baseline at its cap makes room before it adds rather than growing past the cap and trimming after:
        /// storage a dictionary grows into is never given back, so overshooting even once would cost it for good.
        /// </summary>
        [TestMethod]
        public void BaselineAtItsCapDoesNotGrowPastIt()
        {
            var baseline = new Baseline { MaxEntries = 1000 };
            for (var collection = 0; collection < 5; collection++)
            {
                var result = new QueryStatsResult { SnapshotDateUtc = PreviousCollection.AddMinutes(5 * collection) };
                for (var i = 0; i < 600; i++)
                {
                    var key = BaselineKey.Create(BitConverter.GetBytes(collection * 1000 + i), 0, 0);
                    result.PendingBaseline.Add(new(key, new BaselineEntry { LastSeenTicks = result.SnapshotDateUtc.Ticks }));
                }
                baseline.Apply(result);
            }

            Assert.AreEqual(1000, baseline.Count);
            Assert.AreEqual(600, baseline.Evictions, "a collection at the cap makes room for all of its new rows");
            Assert.IsTrue(baseline.Capacity < 1500, $"storage stays near the cap rather than doubling past it: {baseline.Capacity}");
        }

        [TestMethod]
        public void LookbackIsNeverShorterThanThreeScheduleIntervals()
        {
            var configured = TimeSpan.FromMinutes(60);

            Assert.AreEqual(configured, Baseline.GetMaxLookback(configured, TimeSpan.FromMinutes(5)),
                "a five minute schedule keeps the configured hour");
            Assert.AreEqual(TimeSpan.FromHours(3), Baseline.GetMaxLookback(configured, TimeSpan.FromHours(1)),
                "an hourly schedule tolerates a couple of missed runs rather than discarding the baseline every time");
            Assert.AreEqual(configured, Baseline.GetMaxLookback(configured, null),
                "and a schedule that can't be read leaves the setting as it is");
        }

        [TestMethod]
        public void ScheduleIntervalComesFromTheScheduleTheConnectionRunsOn()
        {
            var config = new CollectionConfig();
            var source = new DBADashSource("Data Source=QueryStatsTest;Integrated Security=True");

            Assert.AreEqual(TimeSpan.FromMinutes(5), config.GetMaxScheduleInterval(source, CollectionType.QueryStats),
                "the default schedule");

            source.CollectionSchedules = new CollectionSchedules
            {
                { CollectionType.QueryStats, new CollectionSchedule { Schedule = "0 0 0/1 * * ?" } }
            };
            Assert.AreEqual(TimeSpan.FromHours(1), config.GetMaxScheduleInterval(source, CollectionType.QueryStats),
                "the connection's own schedule wins");
        }
    }
}
