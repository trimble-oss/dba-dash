using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DBADash.QueryPlan.Test.InlinePlan;

namespace DBADash.QueryPlan.Test
{
    [TestClass]
    public class PlanOperatorRowsInTests
    {
        [TestMethod]
        public void RowsIn_IsWhatTheInputsHandedOver()
        {
            // A join of 10 and 200 rows returning 50.
            var statement = Parse(Op(0, "Hash Match", "Inner Join", Rows(50),
                Op(1, "Table Scan", "Table Scan", Rows(10)),
                Op(2, "Table Scan", "Table Scan", Rows(200))));

            var join = statement.RootOperator!;
            Assert.AreEqual(210, join.RowsIn);
            Assert.AreEqual(-160, join.RowsDiff);
        }

        [TestMethod]
        public void AJoinThatMultipliesRows_HasAPositiveDiff()
        {
            var statement = Parse(Op(0, "Nested Loops", "Inner Join", Rows(5000),
                Op(1, "Table Scan", "Table Scan", Rows(10)),
                Op(2, "Table Scan", "Table Scan", Rows(20))));

            Assert.AreEqual(4970, statement.RootOperator!.RowsDiff);
        }

        [TestMethod]
        public void AnOperatorWithNoInputs_TakesInNothing_SoItsDiffIsTheRowsItIntroduced()
        {
            // Rows read is a separate figure: the scan read 1,000 rows from storage, but brought 5 into the plan.
            var statement = Parse("""
                <RelOp NodeId="0" PhysicalOp="Table Scan" LogicalOp="Table Scan" EstimateRows="1" AvgRowSize="100" EstimatedTotalSubtreeCost="1">
                  <RunTimeInformation><RunTimeCountersPerThread Thread="0" ActualRows="5" ActualRowsRead="1000" ActualExecutions="1" /></RunTimeInformation>
                </RelOp>
                """);

            Assert.AreEqual(0, statement.RootOperator!.RowsIn);
            Assert.AreEqual(5, statement.RootOperator.RowsDiff);
        }

        [TestMethod]
        public void OnAnEstimatedPlan_TheEstimatesForAllExecutionsAreUsed()
        {
            var statement = Parse(Op(0, "Filter", "Filter", 3, "",
                Op(1, "Table Scan", "Table Scan", 40, "")));

            Assert.AreEqual(40, statement.RootOperator!.RowsIn);
            Assert.AreEqual(-37, statement.RootOperator.RowsDiff);

            var scan = statement.Operators.Single(op => op.NodeId == 1);
            Assert.AreEqual(0, scan.RowsIn);
            Assert.AreEqual(40, scan.RowsDiff);
        }
    }
}
