using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DBADash.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// SlowQueries is never run by a triggered collection - see <see cref="CollectionMessage.SlowQueriesNotTriggerable"/>.
    /// It is skipped when other collections were asked for too, and rejected when it was the only one.  None of this
    /// reaches an instance: the decision is made before the collector connects.
    /// </summary>
    [TestClass]
    public class SlowQueriesOnDemandTests
    {
        private const string SqlConnection = "Data Source=SQL1;Integrated Security=SSPI";

        private static DBADashSource Source() =>
            new()
            {
                SourceConnection = new DBADashConnection(SqlConnection),
                ConnectionID = "SQL1",
                SlowQueryThresholdMs = 1000
            };

        [TestMethod]
        [DataRow("SlowQueries")]
        [DataRow("slowqueries")]
        public void SlowQueries_IsSkipped_AndTheRestStillRun(string requested)
        {
            var message = new CollectionMessage(new List<string> { "CPU", requested, "Waits" }, "SQL1");

            var (standard, custom, unknown) = message.ParseCollectionTypes(Source(), new CollectionConfig());

            Assert.IsTrue(message.SlowQueriesSkipped);
            CollectionAssert.AreEqual(new[] { CollectionType.CPU, CollectionType.Waits }, standard);
            Assert.AreEqual(0, custom.Count);
            Assert.AreEqual(0, unknown.Count, "Skipped deliberately, so not reported as an unknown type");
        }

        [TestMethod]
        public void OtherCollections_AreUnaffected()
        {
            var message = new CollectionMessage(new List<string> { "CPU" }, "SQL1");

            var (standard, _, _) = message.ParseCollectionTypes(Source(), new CollectionConfig());

            Assert.IsFalse(message.SlowQueriesSkipped);
            CollectionAssert.AreEqual(new[] { CollectionType.CPU }, standard);
        }

        [TestMethod]
        public async Task SlowQueriesAlone_IsRejectedWithTheReason()
        {
            var cfg = new CollectionConfig();
            cfg.SourceConnections.Add(Source());
            var message = new CollectionMessage(new List<string> { "SlowQueries" }, "SQL1");

            var ex = await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
                message.CollectAsync(cfg, "SQL1", null, CancellationToken.None));

            Assert.AreEqual(CollectionMessage.SlowQueriesNotTriggerable, ex.Message);
        }
    }
}
