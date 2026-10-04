using System.Linq;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Test
{
    [TestClass]
    public class PlanOperatorFilterTests
    {
        private static PlanStatement KeyLookup => TestPlans.Load(TestPlans.KeyLookupSeek).Statements[0];

        private static PlanStatement ParallelSpill => TestPlans.Load(TestPlans.ParallelSpill).Statements[0];

        private static int[] NodeIds(PlanOperatorFilter filter, PlanStatement statement) =>
            filter.Apply(statement).Select(op => op.NodeId).OrderBy(id => id).ToArray();

        [TestMethod]
        public void Empty_ShowsEveryOperator()
        {
            var statement = KeyLookup;
            var filter = new PlanOperatorFilter();

            Assert.IsTrue(filter.IsEmpty);
            Assert.AreEqual(statement.Operators.Count(), filter.Apply(statement).Count());
        }

        [TestMethod]
        public void ReadingData_TakesSeeksAndLookupsButNotJoins()
        {
            // Nested Loops (0) over an Index Seek (1) and a key lookup (2).
            var filter = new PlanOperatorFilter { Groups = [PlanOperatorGroup.ReadingData] };

            CollectionAssert.AreEqual(new[] { 1, 2 }, NodeIds(filter, KeyLookup));
        }

        [TestMethod]
        public void Seeks_LeaveOutTheSeekThatIsALookup()
        {
            CollectionAssert.AreEqual(new[] { 1 }, NodeIds(new PlanOperatorFilter { Groups = [PlanOperatorGroup.Seeks] }, KeyLookup));
            CollectionAssert.AreEqual(new[] { 2 }, NodeIds(new PlanOperatorFilter { Groups = [PlanOperatorGroup.Lookups] }, KeyLookup));
        }

        [TestMethod]
        public void SeveralGroups_ShowAnOperatorInAnyOfThem()
        {
            var filter = new PlanOperatorFilter { Groups = [PlanOperatorGroup.Joins, PlanOperatorGroup.Lookups] };

            CollectionAssert.AreEqual(new[] { 0, 2 }, NodeIds(filter, KeyLookup));
        }

        [TestMethod]
        public void Scans_TakeTableAndIndexScans()
        {
            // Clustered Index Scan (3) and Table Scan (4) under a sort, a hash join and an exchange.
            var filter = new PlanOperatorFilter { Groups = [PlanOperatorGroup.Scans] };

            CollectionAssert.AreEqual(new[] { 3, 4 }, NodeIds(filter, ParallelSpill));
        }

        [TestMethod]
        public void AnOperatorCanBeInSeveralGroups()
        {
            var seek = KeyLookup.Operators.Single(op => op.NodeId == 1);

            CollectionAssert.AreEquivalent(
                new[] { PlanOperatorGroup.ReadingData, PlanOperatorGroup.Seeks },
                PlanOperatorGroup.Of(seek).ToArray());
        }

        [TestMethod]
        public void InsightsOnly_KeepsTheOperatorsWithInsights()
        {
            // The sort and the hash join spilled.
            var filter = new PlanOperatorFilter { InsightsOnly = true };

            CollectionAssert.AreEqual(new[] { 1, 2 }, NodeIds(filter, ParallelSpill));
        }

        [TestMethod]
        public void InsightsOnly_CountsMoreThanWarnings()
        {
            // No warnings in this plan, but a missing index on the table the operators read - which is
            // an insight the properties panel shows on them, so they are kept.
            var statement = KeyLookup;
            var kept = new PlanOperatorFilter { InsightsOnly = true }.Apply(statement).ToList();

            Assert.IsFalse(statement.Operators.Any(op => op.HasWarnings));
            Assert.IsTrue(kept.Count > 0);
            CollectionAssert.AreEquivalent(
                statement.Operators.Where(op => PlanInsights.ForOperator(op, statement).Count > 0).ToList(),
                kept);
        }

        [TestMethod]
        public void OperatorName_MatchesTheCaption()
        {
            var filter = new PlanOperatorFilter { OperatorName = "index seek" };

            CollectionAssert.AreEqual(new[] { 1 }, NodeIds(filter, KeyLookup));
        }

        [TestMethod]
        public void Text_FindsTheIndexWithOrWithoutBrackets()
        {
            CollectionAssert.AreEqual(new[] { 1 }, NodeIds(new PlanOperatorFilter { Text = "ix_orders_customerid" }, KeyLookup));
            CollectionAssert.AreEqual(new[] { 1 }, NodeIds(new PlanOperatorFilter { Text = "[IX_Orders_CustomerID]" }, KeyLookup));
        }

        [TestMethod]
        public void Text_ThatIsANumber_FindsThatNodeOnly()
        {
            CollectionAssert.AreEqual(new[] { 2 }, NodeIds(new PlanOperatorFilter { Text = " 2 " }, KeyLookup));
        }

        [TestMethod]
        public void Conditions_AllHaveToHold()
        {
            var filter = new PlanOperatorFilter { Groups = [PlanOperatorGroup.ReadingData], Text = "Nested" };

            Assert.AreEqual(0, filter.Apply(KeyLookup).Count());
        }
    }
}
