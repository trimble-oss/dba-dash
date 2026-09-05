using System.Linq;
using DBADash.Deadlock.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// What an AI analysis is given.  The payload leaves the estate, so these tests are as much about
    /// what it contains being visible to the user as about it being correct.
    /// </summary>
    [TestClass]
    public class DeadlockAnalysisPayloadTests
    {
        private static DeadlockAnalysisPayload Build(string sampleName, string instance = "SQLPROD01")
        {
            var graph = DeadlockParser.Parse(TestGraphs.Load(sampleName)).First();
            return DeadlockAnalysisPayload.Build(graph, DeadlockAnalyser.Analyse(graph), instance);
        }

        [TestMethod]
        public void Payload_CarriesTheGraphAndWhatWeAlreadyKnow()
        {
            var payload = Build(TestGraphs.SharedLock);

            StringAssert.Contains(payload.GraphXml, "<deadlock");
            Assert.IsTrue(payload.Findings.Count > 0, "The findings are what stop the model rediscovering them.");
            Assert.IsTrue(payload.Findings.Any(f => f.Contains("opposite order")));
            StringAssert.StartsWith(payload.Signature, "0x");
            Assert.AreEqual("SQLPROD01", payload.Instance);
        }

        [TestMethod]
        public void Payload_NamesTheObjectsAndModulesForALaterSchemaEnrichment()
        {
            var payload = Build(TestGraphs.SharedLock);

            CollectionAssert.Contains(payload.Objects.ToArray(), "Ops.dbo.Customer (PK_Customer)");
            CollectionAssert.Contains(payload.Modules.ToArray(), "Ops.dbo.usp_PostInvoice");
        }

        [TestMethod]
        public void Payload_DescribesEachParticipant()
        {
            var payload = Build(TestGraphs.SharedLock);

            Assert.AreEqual(3, payload.Participants.Count);

            var victim = payload.Participants.Single(p => p.Contains("victim"));
            StringAssert.Contains(victim, "SPID 55");
            StringAssert.Contains(victim, "database Ops");
            StringAssert.Contains(victim, "log used");
        }

        [TestMethod]
        public void Preview_ShowsEveryFieldThatWouldBeSent()
        {
            // The preview is the consent step: anything sent but not shown would make it a lie.
            var payload = Build(TestGraphs.SharedLock);
            var preview = payload.ToPreview();

            StringAssert.Contains(preview, payload.Signature);
            StringAssert.Contains(preview, payload.Instance);
            StringAssert.Contains(preview, payload.GraphXml);

            foreach (var value in payload.Participants
                         .Concat(payload.Objects)
                         .Concat(payload.Modules)
                         .Concat(payload.Findings))
            {
                StringAssert.Contains(preview, value);
            }
        }

        [TestMethod]
        public void Preview_WarnsThatStatementsCarryParameterValues()
        {
            var preview = Build(TestGraphs.SharedLock).ToPreview();

            // The graph in this sample really does carry one, which is the point.
            StringAssert.Contains(preview, "parameter values");
            StringAssert.Contains(preview, "@CustomerID = 8812");
        }

        [TestMethod]
        public void Payload_WorksWithoutAnInstance()
        {
            // A graph opened from a file has no instance, and must still be analysable.
            var graph = DeadlockParser.Parse(TestGraphs.Load(TestGraphs.KeyLock)).First();
            var payload = DeadlockAnalysisPayload.Build(graph, DeadlockAnalyser.Analyse(graph));

            Assert.IsNull(payload.Instance);
            Assert.IsFalse(payload.ToPreview().Contains("Instance:"));
        }
    }
}
