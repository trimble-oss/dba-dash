using System;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The live profiles the running queries collection keeps - see <see cref="LiveProfileCollector"/>.
    /// </summary>
    [TestClass]
    public class LiveProfileTests
    {
        [TestMethod]
        public void CountersRoundTrip()
        {
            var profiles = new DataTable("Profiles");
            profiles.Columns.Add("session_id", typeof(int));
            foreach (var column in LiveProfileCounters.Columns) profiles.Columns.Add(column, typeof(long));

            profiles.Rows.Add(Row(profiles, 52, nodeId: 0, thread: 0, rows: 10, readRows: null));
            profiles.Rows.Add(Row(profiles, 52, nodeId: 1, thread: 3, rows: 1234567890123, readRows: 99));
            profiles.Rows.Add(Row(profiles, 60, nodeId: 0, thread: 0, rows: 5, readRows: 1));

            var decoded = LiveProfileCounters.Decode(LiveProfileCounters.Encode(profiles, r => (int)r["session_id"] == 52));

            CollectionAssert.AreEqual(LiveProfileCounters.Columns, decoded.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
            Assert.AreEqual(2, decoded.Rows.Count, "Only the rows for the session asked for are packed.");
            Assert.AreEqual(1234567890123L, decoded.Rows[1]["row_count"]);
            Assert.AreEqual(3L, decoded.Rows[1]["thread_id"]);
            Assert.AreEqual(99L, decoded.Rows[1]["actual_read_row_count"]);
            Assert.AreEqual(DBNull.Value, decoded.Rows[0]["actual_read_row_count"], "A null counter stays null rather than becoming 0.");
        }

        [TestMethod]
        public void ColumnsMissingFromTheRowsArePackedAsNull()
        {
            // An instance without actual_read_row_count doesn't have the column at all
            var profiles = new DataTable("Profiles");
            profiles.Columns.Add("node_id", typeof(int));
            profiles.Columns.Add("thread_id", typeof(int));
            profiles.Columns.Add("row_count", typeof(long));
            profiles.Rows.Add(4, 0, 7L);

            var decoded = LiveProfileCounters.Decode(LiveProfileCounters.Encode(profiles, _ => true));

            Assert.AreEqual(7L, decoded.Rows[0]["row_count"]);
            Assert.AreEqual(DBNull.Value, decoded.Rows[0]["actual_read_row_count"]);
        }

        /// <summary>A table-valued parameter binds by ordinal, so the collection's table must list the type's columns in order.</summary>
        [TestMethod]
        public void CollectionTableMatchesTableType()
        {
            var path = Path.Combine(MessageAuthorizationTests.FindRepositoryRoot(), "DBADashDB", "dbo", "User Defined Types", "RunningQueryLiveProfiles.sql");
            var expected = DeadlockTableContractTests.ParseTableTypeColumns(File.ReadAllText(path));
            var actual = LiveProfileCollector.NewTable().Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            Assert.AreEqual(string.Join(", ", expected), string.Join(", ", actual), "dbo.RunningQueryLiveProfiles and LiveProfileCollector.NewTable have drifted.");
        }

        /// <summary>
        /// A MARS session's request is chosen as the import chooses it - highest CPU - before the thresholds, so a capture
        /// always has the snapshot row it is opened from.
        /// </summary>
        [TestMethod]
        public void SelectsTheRequestTheImportKeeps()
        {
            var snapshot = DateTime.UtcNow;
            var dt = new DataTable("RunningQueries");
            dt.Columns.Add("SnapshotDateUTC", typeof(DateTime));
            dt.Columns.Add("session_id", typeof(short));
            dt.Columns.Add("start_time_utc", typeof(DateTime));
            dt.Columns.Add("sql_handle", typeof(byte[]));
            dt.Columns.Add("plan_handle", typeof(byte[]));
            dt.Columns.Add("query_plan_hash", typeof(byte[]));
            dt.Columns.Add("statement_start_offset", typeof(int));
            dt.Columns.Add("statement_end_offset", typeof(int));
            dt.Columns.Add("cpu_time", typeof(int));
            dt.Columns.Add("blocking_session_id", typeof(short));

            void Add(short session, int cpu, short blockedBy, byte id) =>
                dt.Rows.Add(snapshot, session, snapshot.AddMinutes(-10), new byte[] { id }, new byte[] { id }, new byte[] { 1, 2, 3, 4, 5, 6, 7, id }, 0, -1, cpu, blockedBy);

            // Session 60: the highest CPU request is blocked and under the CPU threshold, so nothing is captured - the
            // other request would qualify on duration, but the import doesn't keep it
            Add(60, 50000, 99, 1);
            Add(60, 10000, 0, 2);
            // Session 61: qualifies on duration
            Add(61, 20000, 0, 3);

            var planKeys = dt.AsEnumerable()
                .Select(r => new Plan((byte[])r["plan_handle"], (byte[])r["query_plan_hash"], 0, -1).Key)
                .ToHashSet();
            var threshold = new LiveProfileThreshold { CPUThreshold = 60000, DurationThreshold = 300000 };

            var requests = LiveProfileCollector.SelectRequests(dt, planKeys, threshold);

            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual((short)61, requests[0].SessionID);
        }

        [TestMethod]
        public void ThresholdIsOffUntilOneIsSet()
        {
            Assert.IsFalse(new LiveProfileThreshold().Enabled);
            Assert.IsTrue(LiveProfileThreshold.DefaultThreshold.Enabled);
            Assert.IsFalse(new LiveProfileThreshold { CPUThreshold = 1000, MaxPerCollection = 0 }.Enabled, "A limit of 0 per collection captures nothing.");
        }

        [TestMethod]
        public void LiveProfilesNeedPlanCollection()
        {
            var src = new DBADashSource("Data Source=.;Integrated Security=SSPI")
            {
                RunningQueryLiveProfileThreshold = LiveProfileThreshold.DefaultThreshold
            };
            Assert.IsFalse(src.LiveProfileCollectionEnabled, "Without plan collection there is no plan to show the counters on.");

            src.PlanCollectionEnabled = true;
            Assert.IsTrue(src.LiveProfileCollectionEnabled);
        }

        /// <summary>The plan collection properties the connections grid edits - a cleared cell sets null.</summary>
        [TestMethod]
        public void PlanCollectionGridPropertiesTakeNull()
        {
            var src = new DBADashSource("Data Source=.;Integrated Security=SSPI");
            src.PlanCollectionCPUThreshold = null;
            Assert.IsNull(src.RunningQueryPlanThreshold, "Clearing a cell on a source without plan collection leaves it off.");

            src.PlanCollectionEnabled = true;
            Assert.AreEqual(PlanCollectionThreshold.DefaultThreshold.CPUThreshold, src.PlanCollectionCPUThreshold);

            src.PlanCollectionCPUThreshold = null;
            Assert.IsNull(src.PlanCollectionCPUThreshold, "A threshold that isn't used shows blank.");
            Assert.AreEqual(int.MaxValue, src.RunningQueryPlanThreshold.CPUThreshold);
            Assert.IsTrue(src.PlanCollectionEnabled, "Still on through the other thresholds.");

            src.PlanCollectionDurationThreshold = null;
            src.PlanCollectionMemoryGrantThreshold = null;
            Assert.IsFalse(src.PlanCollectionEnabled, "With CPU, duration and memory grant cleared there is nothing to collect on.");

            src.PlanCollectionCPUThreshold = 2500;
            Assert.IsTrue(src.PlanCollectionEnabled);
            Assert.AreEqual(2500, src.RunningQueryPlanThreshold.CPUThreshold);
        }

        /// <summary>The properties the service config tool's connections grid edits.</summary>
        [TestMethod]
        public void GridPropertiesSetTheThreshold()
        {
            var src = new DBADashSource("Data Source=.;Integrated Security=SSPI");
            Assert.IsFalse(src.LivePlanCaptureEnabled);
            Assert.IsNull(src.LivePlanCaptureCPUThreshold);

            // Clearing a cell on a source without live capture leaves it off rather than adding an empty threshold
            src.LivePlanCaptureCPUThreshold = null;
            src.LivePlanCaptureMaxPerCollection = null;
            Assert.IsNull(src.RunningQueryLiveProfileThreshold);

            src.LivePlanCaptureEnabled = true;
            Assert.AreEqual(LiveProfileThreshold.DefaultThreshold.CPUThreshold, src.LivePlanCaptureCPUThreshold, "Turning it on starts from the defaults.");

            src.LivePlanCaptureDurationThreshold = 1234;
            src.LivePlanCaptureMaxPerCollection = 3;
            Assert.AreEqual(1234, src.RunningQueryLiveProfileThreshold.DurationThreshold);
            Assert.AreEqual(3, src.RunningQueryLiveProfileThreshold.MaxPerCollection);

            src.LivePlanCaptureEnabled = false;
            Assert.IsNull(src.RunningQueryLiveProfileThreshold);

            // A threshold set on its own turns it on, as the plan collection columns do
            src.LivePlanCaptureCPUThreshold = 5000;
            Assert.IsTrue(src.LivePlanCaptureEnabled);
            Assert.IsNull(src.LivePlanCaptureDurationThreshold, "A threshold that isn't used shows blank.");

            // A cleared cell: the threshold isn't used, and the limit goes back to the default
            src.LivePlanCaptureDurationThreshold = 1000;
            src.LivePlanCaptureCPUThreshold = null;
            src.LivePlanCaptureMaxPerCollection = null;
            Assert.AreEqual(int.MaxValue, src.RunningQueryLiveProfileThreshold.CPUThreshold);
            Assert.AreEqual(LiveProfileThreshold.DefaultMaxPerCollection, src.LivePlanCaptureMaxPerCollection);
            Assert.IsTrue(src.LivePlanCaptureEnabled, "Still on through the duration threshold.");

            src.LivePlanCaptureDurationThreshold = null;
            Assert.IsFalse(src.LivePlanCaptureEnabled, "Both thresholds cleared captures nothing.");
        }

        private static object[] Row(DataTable table, int session, int nodeId, int thread, long rows, long? readRows)
        {
            var values = new object[table.Columns.Count];
            for (var i = 0; i < values.Length; i++) values[i] = 0L;
            values[0] = session;
            values[table.Columns["node_id"]!.Ordinal] = (long)nodeId;
            values[table.Columns["thread_id"]!.Ordinal] = (long)thread;
            values[table.Columns["row_count"]!.Ordinal] = rows;
            values[table.Columns["actual_read_row_count"]!.Ordinal] = readRows.HasValue ? readRows.Value : DBNull.Value;
            return values;
        }
    }
}
