using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DBADash.Test
{
    /// <summary>The defaults new source connections start with - the app's own, or the user's saved ones.</summary>
    [TestClass]
    public class NewConnectionDefaultsTests
    {
        private string folder = null!;
        private NewConnectionDefaults store = null!;

        [TestInitialize]
        public void Init()
        {
            folder = Path.Combine(Path.GetTempPath(), "DBADashTest_" + Guid.NewGuid().ToString("N"));
            store = new NewConnectionDefaults(Path.Combine(folder, "NewConnectionDefaults.json"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [TestMethod]
        public void AppDefaults_EnableTheRecommendedCollections()
        {
            var d = SourceConnectionOptions.AppDefaults;
            Assert.AreEqual(SourceConnectionOptions.DefaultSlowQueryThresholdMs, d.SlowQueryThresholdMs);
            Assert.AreEqual(DBADashSource.SlowQueryCaptureModes.EventFile, d.SlowQueryCaptureMode);
            Assert.AreEqual(DBADashSource.ManagedDeadlockXESessionName, d.DeadlockXESessionName);
            Assert.IsTrue(d.SkipDeadlocksOnAzureDB);
            Assert.IsTrue(d.RunningQueryPlanThreshold.PlanCollectionEnabled);
            Assert.IsTrue(d.CollectSessionWaits && d.CollectTaskWaits && d.CollectTempDB && d.CollectCursors);
            Assert.IsTrue(d.RunningQueryLiveProfileThreshold.Enabled);
            Assert.AreEqual(SourceConnectionOptions.DefaultQueryStatsTopN, d.QueryStatsTopN);
            Assert.IsNull(d.Validate());
        }

        [TestMethod]
        public void PropertyInitializers_AreUnchanged_SoExistingConnectionsAndMessagesAreUnaffected()
        {
            // A message or file that leaves an option out must not switch collection on
            var o = JsonConvert.DeserializeObject<SourceConnectionOptions>("{}");
            Assert.AreEqual(-1, o!.SlowQueryThresholdMs);
            Assert.AreEqual(DBADashSource.SlowQueryCaptureModes.RingBuffer, o.SlowQueryCaptureMode);
            Assert.AreEqual(string.Empty, o.DeadlockXESessionName);
            Assert.IsFalse(o.SkipDeadlocksOnAzureDB, "An older GUI's message keeps its old behaviour");
            Assert.IsNull(o.RunningQueryPlanThreshold);
            Assert.IsNull(o.RunningQueryLiveProfileThreshold);
            Assert.AreEqual(0, o.QueryStatsTopN);
            Assert.IsFalse(o.CollectTaskWaits || o.CollectTempDB || o.CollectCursors);
        }

        [TestMethod]
        public void Load_WithoutSavedDefaults_ReturnsAppDefaults()
        {
            Assert.IsFalse(store.IsCustomized);
            Assert.AreEqual(SourceConnectionOptions.AppDefaults.Serialize(), store.Load().Serialize());
        }

        [TestMethod]
        public void SaveLoadReset_RoundTrips()
        {
            var mine = SourceConnectionOptions.AppDefaults;
            mine.SlowQueryThresholdMs = 5000;
            mine.SlowQueryCaptureMode = DBADashSource.SlowQueryCaptureModes.RingBuffer;
            mine.RunningQueryPlanThreshold = null;
            mine.CollectCursors = false;
            mine.DeadlockXESessionName = string.Empty;
            mine.CustomCollections = new Dictionary<string, CustomCollection> { { "Test", new CustomCollection() } };

            store.Save(mine);
            Assert.IsTrue(store.IsCustomized);
            Assert.IsFalse(File.ReadAllText(store.FilePath).Contains(nameof(SourceConnectionOptions.CustomCollections)), "Custom collections aren't saved");

            var loaded = store.Load();
            Assert.AreEqual(5000, loaded.SlowQueryThresholdMs);
            Assert.AreEqual(DBADashSource.SlowQueryCaptureModes.RingBuffer, loaded.SlowQueryCaptureMode);
            Assert.IsNull(loaded.RunningQueryPlanThreshold);
            Assert.IsFalse(loaded.CollectCursors);
            Assert.AreEqual(string.Empty, loaded.DeadlockXESessionName);
            Assert.IsNull(loaded.CustomCollections);

            store.Reset();
            Assert.IsFalse(store.IsCustomized);
            Assert.AreEqual(SourceConnectionOptions.AppDefaults.Serialize(), store.Load().Serialize());
        }

        [TestMethod]
        public void Load_OptionMissingFromSavedFile_UsesAppDefault()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(store.FilePath, "{\"SlowQueryThresholdMs\":2000}");
            var loaded = store.Load();
            Assert.AreEqual(2000, loaded.SlowQueryThresholdMs);
            Assert.IsTrue(loaded.CollectTaskWaits);
            Assert.AreEqual(DBADashSource.SlowQueryCaptureModes.EventFile, loaded.SlowQueryCaptureMode);
        }

        [TestMethod]
        [DataRow("Data Source=myserver.database.windows.net;Initial Catalog=DB1", true)]
        [DataRow("Data Source=tcp:myserver.database.windows.net,1433", true)]
        [DataRow("Data Source=mi.abc123.database.windows.net", true)]
        [DataRow("Data Source=MYSERVER.DATABASE.USGOVCLOUDAPI.NET", true)]
        [DataRow("Data Source=SQL1;Integrated Security=True", false)]
        [DataRow(@"Data Source=SQL1\INST,1433", false)]
        [DataRow("Data Source=mydb.abc.eu-west-1.rds.amazonaws.com", false)]
        [DataRow("Data Source=database.windows.net.example.com", false)]
        public void IsPossiblyAzureSQLHost(string connectionString, bool expected) =>
            Assert.AreEqual(expected, DBADashConnection.IsPossiblyAzureSQLHost(connectionString));

        [TestMethod]
        public void Load_UnreadableFile_ReturnsAppDefaults()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(store.FilePath, "not json");
            Assert.AreEqual(SourceConnectionOptions.AppDefaults.Serialize(), store.Load().Serialize());
        }
    }
}
