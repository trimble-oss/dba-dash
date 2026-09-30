using System.Linq;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// Telling from the plan whether a partitioned scan or seek eliminated partitions, and the card
    /// that says when it did not.  All against dbo.CPU, partitioned by day on a DATETIME2(3) column.
    /// </summary>
    [TestClass]
    public class PlanPartitionEliminationTests
    {
        private static PlanOperator Partitioned(PlanStatement statement) =>
            statement.Operators.Single(o => o.IsPartitioned);

        private static PlanInsight? Card(PlanStatement statement) =>
            PlanInsights.ForStatement(statement).SingleOrDefault(i => i.Text.Contains("partition"));

        [TestMethod]
        public void ScanWithNoPartitionSeek_ReadsEveryPartition()
        {
            var partitions = Partitioned(TestPlans.Statement(TestPlans.PartitionScanConvert)).Partitions!;

            Assert.IsFalse(partitions.HasPartitionSeek);
            Assert.IsTrue(partitions.ReadsEveryPartition);
            Assert.IsFalse(partitions.IsEliminated);
            Assert.AreEqual(381, partitions.PartitionsAccessed);
            CollectionAssert.AreEqual(new[] { new PlanPartitionRange(1, 381) }, partitions.AccessedRanges.ToList());
        }

        [TestMethod]
        public void ScanOfEveryPartition_ComparedWithAConvertToDatetime_IsAWarningNamingTheConvert()
        {
            var statement = TestPlans.Statement(TestPlans.PartitionScanConvert);
            var card = Card(statement);

            Assert.IsNotNull(card);
            Assert.AreEqual(PlanWarningSeverity.Warning, card!.Severity);
            Assert.AreSame(Partitioned(statement), card.Operator);
            StringAssert.StartsWith(card.Text, "This query's scan accessed all 381 partitions of dbo.CPU. Partition elimination is not in effect.");
            StringAssert.Contains(card.Text, "Consider adding a predicate on the partitioning column.");
            StringAssert.Contains(card.Text, "or DATETIME2(7) against DATETIME2(3), prevents elimination even though the query still returns the right rows.");
            StringAssert.Contains(card.Text, "compares against a DATETIME value: CONVERT(datetime,[@1],0)");
            StringAssert.Contains(card.Text, "If that comparison is with the partitioning column, it is the likely cause.");
        }

        [TestMethod]
        public void ConvertToDatetime_FollowedByMoreOfThePredicate_IsNamedWithoutIt()
        {
            // The fixture's conversion ends its predicate; here another condition comes after it.
            var xml = TestPlans.Xml(TestPlans.PartitionScanConvert).Replace(
                "ScalarString=\"[DBADashDB].[dbo].[CPU].[EventTime]&gt;=CONVERT(datetime,[@1],0)\"",
                "ScalarString=\"[DBADashDB].[dbo].[CPU].[EventTime]&gt;=CONVERT(datetime,[@1],0) AND [DBADashDB].[dbo].[CPU].[SQLProcessCPU]&gt;(10)\"");
            var statement = PlanParser.Parse(xml).PrimaryStatement!;
            StringAssert.Contains(Partitioned(statement).Predicate, "AND", "The predicate was replaced.");

            var card = Card(statement);

            Assert.IsNotNull(card);
            StringAssert.Contains(card!.Text, "compares against a DATETIME value: CONVERT(datetime,[@1],0).");
            Assert.IsFalse(card.Text.Contains("SQLProcessCPU"), card.Text);
        }

        [TestMethod]
        public void ScanOfEveryPartition_ComparedWithADatetimeVariable_NamesTheVariable()
        {
            var card = Card(TestPlans.Statement(TestPlans.PartitionScanDatetimeVariable));

            Assert.IsNotNull(card);
            Assert.AreEqual(PlanWarningSeverity.Warning, card!.Severity);
            StringAssert.StartsWith(card.Text, "This query's scan accessed all 381 partitions of dbo.CPU.");
            StringAssert.Contains(card.Text, "compares against a DATETIME value: @t.");
        }

        [TestMethod]
        public void SeekOverAConstantRangeFromTheFirstPartition_IsNotEliminated_ButNotCertainlyEveryPartition()
        {
            var statement = TestPlans.Statement(TestPlans.PartitionSeekDatetimeVariable);
            var partitions = Partitioned(statement).Partitions!;

            Assert.IsTrue(partitions.HasPartitionSeek);
            Assert.IsFalse(partitions.HasDynamicBound);
            Assert.AreEqual(new PlanPartitionRange(1, 381), partitions.ConstantRange);
            Assert.IsFalse(partitions.IsEliminated);
            Assert.IsFalse(partitions.ReadsEveryPartition, "A literal can compile to a constant range from 1 that stops short of the last.");

            var card = Card(statement);
            Assert.IsNotNull(card);
            Assert.AreEqual(PlanWarningSeverity.Warning, card!.Severity);
            StringAssert.StartsWith(card.Text, "This query's seek accessed 381 partitions of dbo.CPU (1 to 381). Partition elimination doesn't appear to be in effect");

            // The seek's range on EventTime is an Expr1004 worked out by a Compute Scalar below it.
            StringAssert.Contains(card.Text, "compares against a DATETIME value: @t, and its seek range is worked out with GetRangeWithMismatchedTypes");
        }

        [TestMethod]
        public void RangePartitionNewOnTheStart_IsElimination()
        {
            var statement = TestPlans.Statement(TestPlans.PartitionElimination);
            var partitions = Partitioned(statement).Partitions!;

            Assert.IsTrue(partitions.HasDynamicBound);
            Assert.IsTrue(partitions.IsEliminated);
            Assert.AreEqual(15, partitions.PartitionsAccessed);
            Assert.IsNull(Card(statement));
        }

        [TestMethod]
        public void RangePartitionNewOnTheEnd_IsElimination_ThoughTheRunReadFromTheFirstPartition()
        {
            var statement = TestPlans.Statement(TestPlans.PartitionEliminationLessThan);
            var partitions = Partitioned(statement).Partitions!;

            CollectionAssert.AreEqual(new[] { new PlanPartitionRange(1, 267) }, partitions.AccessedRanges.ToList());
            Assert.IsTrue(partitions.HasDynamicBound);
            Assert.IsTrue(partitions.IsEliminated);
            Assert.IsNull(Card(statement));
        }

        [TestMethod]
        public void EstimatedPlan_IsJudgedByItsPartitionSeek()
        {
            var statement = TestPlans.Statement(TestPlans.PartitionEliminationLessThanEstimated);
            var partitions = Partitioned(statement).Partitions!;

            Assert.IsFalse(statement.IsActualPlan);
            Assert.IsNull(partitions.PartitionsAccessed);
            Assert.IsTrue(partitions.IsEliminated);
            Assert.IsNull(Card(statement));
        }

        [TestMethod]
        public void OperatorCards_IncludeThePartitionCard()
        {
            var statement = TestPlans.Statement(TestPlans.PartitionScanDatetimeVariable);
            var op = Partitioned(statement);

            Assert.IsTrue(PlanInsights.ForOperator(op, statement).Any(i => i.Text.StartsWith("This query's scan accessed all 381 partitions")));

            // Not on the operators above it.
            Assert.IsFalse(PlanInsights.ForOperator(op.Parent!, statement).Any(i => i.Text.Contains("partitions")));
        }

        [TestMethod]
        public void ValueVector_NamesEachColumnItSets()
        {
            // GetRangeWithMismatchedTypes sets Expr1004, Expr1005 and Expr1003 together.
            var statement = TestPlans.Statement(TestPlans.PartitionSeekDatetimeVariable);

            foreach (var name in new[] { "Expr1003", "Expr1004", "Expr1005" })
            {
                StringAssert.StartsWith(statement.ExpressionNamed(name)?.Definition, "GetRangeWithMismatchedTypes(", name);
            }
        }
    }
}
