using System;
using System.Linq;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DBADash.QueryPlan.Test.InlinePlan;

namespace DBADash.QueryPlan.Test
{
    [TestClass]
    public class PlanStatementSummaryTests
    {
        [TestMethod]
        public void For_SummarisesEveryStatementInOrder()
        {
            var plan = TestPlans.Load(TestPlans.Batch);
            var summaries = PlanStatementSummary.For(plan);

            // Including the conditional with no plan of its own: dropping it would make the batch
            // look shorter than it is.
            Assert.AreEqual(plan.Statements.Count, summaries.Count);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, summaries.Select(s => s.Ordinal).ToArray());

            for (var i = 0; i < summaries.Count; i++)
            {
                Assert.AreSame(plan.Statements[i], summaries[i].Statement);
            }
        }

        [TestMethod]
        public void CostShare_IsTheStatementsShareOfTheWholeBatch()
        {
            var summaries = PlanStatementSummary.For(TestPlans.Load(TestPlans.Batch));

            // 55.5 of a batch totalling 0.001 + 55.5 + 0 + 0.25.
            var expensive = summaries.Single(s => s.Statement.StatementId == 2);
            Assert.AreEqual(55.5 / 55.751, expensive.CostShare, 0.00001);

            Assert.AreEqual(1, summaries.Sum(s => s.CostShare), 0.00001, "Shares of one batch add up to all of it.");
        }

        [TestMethod]
        public void Warnings_CountThePlansOwnAndEveryOperators()
        {
            var summaries = PlanStatementSummary.For(TestPlans.Load(TestPlans.Batch));

            var crossJoin = summaries.Single(s => s.Statement.StatementId == 2);
            Assert.AreEqual(1, crossJoin.WarningCount);
            Assert.IsTrue(crossJoin.HasCriticalWarning, "A missing join predicate is critical.");

            var trivial = summaries.Single(s => s.Statement.StatementId == 1);
            Assert.AreEqual(0, trivial.WarningCount);
            Assert.IsFalse(trivial.HasCriticalWarning);
        }

        [TestMethod]
        public void Text_IsCollapsedOntoOneLine()
        {
            var summary = PlanStatementSummary.For(TestPlans.Load(TestPlans.ParallelSpill)).Single();

            // A list row of a few lines should show SQL, not indentation.
            Assert.IsFalse(summary.Text.Contains('\n'));
            Assert.IsFalse(summary.Text.Contains("  ", StringComparison.Ordinal));
            StringAssert.StartsWith(summary.Text, "SELECT c.Name");
        }

        [TestMethod]
        public void ActualFigures_ComeFromTheRuntimeCounters()
        {
            var summary = PlanStatementSummary.For(TestPlans.Load(TestPlans.ParallelSpill)).Single();

            Assert.IsTrue(summary.IsActual);
            Assert.AreEqual(1000, summary.ActualRows, "What the outermost operator returned.");
            Assert.AreEqual(1500, summary.ElapsedMs);
            Assert.AreEqual(3200, summary.CpuMs);
            Assert.AreEqual(8192, summary.GrantedMemoryKb);
            Assert.AreEqual(1024, summary.UsedMemoryKb);
            Assert.AreEqual(4, summary.DegreeOfParallelism);
            Assert.AreEqual(1, summary.ElapsedShare, "The only statement is the slowest one.");

            // The clustered index scan returned 10,000 rows against an estimate of 100.
            Assert.AreEqual(100, summary.WorstEstimateError!.Value, 0.0001);
        }

        [TestMethod]
        public void ActualFigures_AreAbsentOnAnEstimatedPlan()
        {
            var summary = PlanStatementSummary.For(TestPlans.Load(TestPlans.KeyLookupSeek)).Single();

            // Absent rather than zero: nothing was measured, which is a different thing from
            // nothing having happened.
            Assert.IsFalse(summary.IsActual);
            Assert.IsNull(summary.ActualRows);
            Assert.IsNull(summary.ElapsedMs);
            Assert.IsNull(summary.WorstEstimateError);
            Assert.AreEqual(0, summary.ElapsedShare);
        }

        [TestMethod]
        public void WorstEstimateError_CountsAnOperatorThatReturnedNothing()
        {
            // A hundred thousand rows expected and none returned is the worst estimate in the plan,
            // not an absent one - the list ranks statements by this, so passing over it hid the
            // statement most worth looking at.
            var plan = ParsePlan(
                Op(0, "Top", "Top", 1, Rows(1),
                    Op(1, "Clustered Index Scan", "Clustered Index Scan", 100_000, Rows(0))));

            Assert.AreEqual(100_000, PlanStatementSummary.For(plan).Single().WorstEstimateError!.Value, 0.0001);
        }

        [TestMethod]
        public void MissingIndexes_ReportTheCountAndTheBestImpact()
        {
            var summary = PlanStatementSummary.For(TestPlans.Load(TestPlans.KeyLookupSeek)).Single();

            Assert.AreEqual(1, summary.MissingIndexCount);
            Assert.AreEqual(92.4, summary.BestMissingIndexImpact!.Value, 0.001);
            Assert.AreEqual(3, summary.OperatorCount);
        }

        [TestMethod]
        public void AStatementWithNoPlanStillHasASummary()
        {
            var summaries = PlanStatementSummary.For(TestPlans.Load(TestPlans.Batch));
            var conditional = summaries.Single(s => !s.Statement.HasPlan);

            Assert.AreEqual(0, conditional.OperatorCount);
            Assert.AreEqual(0, conditional.CostShare);
            Assert.IsNull(conditional.BestMissingIndexImpact);
        }

        [TestMethod]
        public void For_RejectsANullPlan()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => PlanStatementSummary.For(null!));
        }
    }
}
