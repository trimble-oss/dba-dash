using System.Linq;
using DBADash.QueryPlan.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// What an AI analysis of a plan is given.  The payload leaves the estate, so these tests are as
    /// much about what it contains being visible to the user as about it being correct.
    /// </summary>
    [TestClass]
    public class PlanAnalysisPayloadTests
    {
        private static PlanAnalysisPayload Build(string sampleName, bool includePlanXml = true, string instance = "SQLPROD01")
        {
            var plan = TestPlans.Load(sampleName);
            var statement = plan.PrimaryStatement ?? plan.Statements[0];

            return PlanAnalysisPayload.Build(plan, statement, instance, fileName: sampleName + ".sqlplan", includePlanXml);
        }

        [TestMethod]
        public void Payload_CarriesThePlanAndWhatWeAlreadyKnow()
        {
            var payload = Build(TestPlans.KeyLookupSeek);

            StringAssert.Contains(payload.PlanXml, "<QueryPlan");
            Assert.IsTrue(payload.Insights.Count > 0, "The insights are what stop the model rediscovering them.");
            Assert.IsTrue(payload.Operators.Count > 0, "The operator ranking is the part the XML makes hard to see.");
            StringAssert.StartsWith(payload.Signature, "0x");
            StringAssert.StartsWith(payload.PlanHash, "0x");
            Assert.AreEqual("SQLPROD01", payload.Instance);
        }

        [TestMethod]
        public void Payload_RanksOperatorsByTheirOwnCostRatherThanTheirSubtree()
        {
            // Subtree cost puts 100% on the root of every plan, which tells nobody anything - so the
            // first operator named must not simply be the root.
            var payload = Build(TestPlans.KeyLookupSeek);

            StringAssert.StartsWith(payload.Operators[0], "node ");
            Assert.IsFalse(payload.Operators[0].Contains("node 0 SELECT"),
                "Ranking by subtree cost would always put the root first.");
        }

        [TestMethod]
        public void Payload_IsAboutOneStatementOfABatchRatherThanTheDocument()
        {
            var plan = TestPlans.Load(TestPlans.Batch);
            Assert.IsTrue(plan.Statements.Count > 1, "The batch sample is meant to hold several statements.");

            var first = PlanAnalysisPayload.Build(plan, plan.Statements[0]);
            var second = PlanAnalysisPayload.Build(plan, plan.Statements[1]);

            Assert.AreNotEqual(first.StatementText, second.StatementText);
            Assert.AreNotEqual(first.PlanXml, second.PlanXml);
            Assert.AreNotEqual(first.Signature, second.Signature,
                "Two statements of a batch must not share one identity, or an answer about one is offered for the other.");
        }

        [TestMethod]
        public void Payload_LeavesThePlanXmlOutWhenTheReaderSaysSoAndSaysWhy()
        {
            var payload = Build(TestPlans.KeyLookupSeek, includePlanXml: false);

            Assert.IsFalse(payload.PlanXmlIncluded);
            Assert.IsNotNull(payload.PlanXmlOmittedReason);

            // The summary still goes, which is the point of being able to switch the XML off.
            Assert.IsTrue(payload.Operators.Count > 0);
            StringAssert.Contains(payload.ToPreview(), payload.PlanXmlOmittedReason);
        }

        [TestMethod]
        public void Payload_VersionSaysWhetherTheXmlWentWithIt()
        {
            // A stored answer has to be readable later for what it was produced from.
            StringAssert.Contains(Build(TestPlans.KeyLookupSeek).Version, "xml");
            Assert.IsFalse(Build(TestPlans.KeyLookupSeek, includePlanXml: false).Version.Contains("xml"));
        }

        [TestMethod]
        public void Preview_ShowsEveryFieldThatWouldBeSent()
        {
            // The preview is the consent step: anything sent but not shown would make it a lie.
            var payload = Build(TestPlans.ParallelSpill);
            var preview = payload.ToPreview();

            StringAssert.Contains(preview, payload.Signature);
            StringAssert.Contains(preview, payload.PlanHash);
            StringAssert.Contains(preview, payload.Instance);
            StringAssert.Contains(preview, payload.FileName);
            StringAssert.Contains(preview, payload.PlanXml);

            foreach (var value in payload.Context
                         .Concat(payload.Statistics)
                         .Concat(payload.Insights)
                         .Concat(payload.MissingIndexes)
                         .Concat(payload.Operators)
                         .Concat(payload.Waits)
                         .Concat(payload.Parameters)
                         .Concat(payload.Objects))
            {
                StringAssert.Contains(preview, value);
            }
        }

        [TestMethod]
        public void Preview_WarnsThatThePlanCarriesTheQueryAsItRan()
        {
            StringAssert.Contains(Build(TestPlans.KeyLookupSeek).ToPreview(), "literal values");
        }

        [TestMethod]
        public void Identity_PrefersSqlServersOwnHashesAndSaysWhenItCouldNot()
        {
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);
            var identity = PlanIdentity.For(statement, statement.Xml);

            // The samples are real plans, so they carry both.
            Assert.IsTrue(identity.QueryFromServer);
            Assert.IsTrue(identity.PlanFromServer);
            StringAssert.Contains(identity.Components, "from the plan");
        }

        [TestMethod]
        public void Identity_FallsBackToItsOwnHashAndIsStable()
        {
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);
            var hashes = statement.QueryHash;

            try
            {
                statement.QueryHash = null;
                statement.QueryPlanHash = "not a hash";

                var first = PlanIdentity.For(statement, statement.Xml);
                var second = PlanIdentity.For(statement, statement.Xml);

                Assert.IsFalse(first.QueryFromServer);
                Assert.IsFalse(first.PlanFromServer);
                StringAssert.StartsWith(first.Query, "0x");
                Assert.AreEqual(18, first.Query.Length, "Eight bytes of hex, as SQL Server's own are.");
                Assert.AreEqual(first.Query, second.Query, "The same plan has to be found again next time.");
                Assert.AreEqual(first.Plan, second.Plan);
            }
            finally
            {
                statement.QueryHash = hashes;
            }
        }
    }
}
