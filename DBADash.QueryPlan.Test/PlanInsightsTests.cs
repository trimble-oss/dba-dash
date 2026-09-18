using System.Linq;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DBADash.QueryPlan.Test.InlinePlan;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// What the properties panel says about a statement with nothing selected, and about one
    /// operator: warnings, missing indexes and what the statement's properties show.
    /// </summary>
    [TestClass]
    public class PlanInsightsTests
    {
        [TestMethod]
        public void Statement_ListsEveryOperatorsWarnings_WorstFirst_WithTheOperatorToGoTo()
        {
            var statement = TestPlans.Statement(TestPlans.ParallelSpill);
            var insights = PlanInsights.ForStatement(statement);

            var spill = insights.First(i => i.Text.Contains("spilled"));
            Assert.IsNotNull(spill.Operator, "A warning on an operator says which, so the card can link to it.");
            Assert.IsTrue(spill.Operator!.Warnings.Any(), "Linked to the operator the warning is on.");

            // Severity never goes up down the list.
            var severities = insights.Select(i => (int)i.Severity).ToList();
            CollectionAssert.AreEqual(severities.OrderByDescending(s => s).ToList(), severities);
        }

        [TestMethod]
        public void Statement_ListsMissingIndexes_WithTheirScriptAndTheOperatorReadingTheTable()
        {
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);
            var insight = PlanInsights.ForStatement(statement).Single(i => i.MissingIndex is not null);

            StringAssert.StartsWith(insight.Text, "Missing index");
            StringAssert.Contains(insight.MissingIndex!.CreateStatement, "CREATE");
            Assert.IsNotNull(insight.Operator);
            CollectionAssert.Contains(statement.MissingIndexesFor(insight.Operator!).ToList(), insight.MissingIndex);

            // And on that operator's own cards.
            var own = PlanInsights.ForOperator(insight.Operator!, statement);
            Assert.IsTrue(own.Any(i => ReferenceEquals(i.MissingIndex, insight.MissingIndex)));
        }

        [TestMethod]
        public void Statement_SaysWhenThePlanIsAnEstimate_AndWhenTheOptimizerTimedOut()
        {
            var estimated = Parse(Op(1, "Table Scan", "Table Scan", ""));
            Assert.IsTrue(PlanInsights.ForStatement(estimated).Any(i => i.Text.StartsWith("This is an estimated plan")));

            var measured = Parse(Op(1, "Table Scan", "Table Scan", Rows(10)));
            Assert.IsFalse(PlanInsights.ForStatement(measured).Any(i => i.Text.StartsWith("This is an estimated plan")));

            estimated.OptimisationEarlyAbortReason = "TimeOut";
            var timeout = PlanInsights.ForStatement(estimated).First();
            Assert.AreEqual(PlanWarningSeverity.Warning, timeout.Severity, "A time out is worth more than a note, so it comes first.");
            StringAssert.StartsWith(timeout.Text, "The optimizer timed out");

            estimated.OptimisationEarlyAbortReason = "GoodEnoughPlanFound";
            Assert.IsFalse(PlanInsights.ForStatement(estimated).Any(i => i.Text.Contains("optimizer timed out")), "Finding a good enough plan is normal.");
        }

        [TestMethod]
        public void NonParallelReasons_ReadAsWords()
        {
            Assert.AreEqual("max DOP set to one", PlanInsights.Words("MaxDOPSetToOne"));
            Assert.AreEqual("estimated DOP is one", PlanInsights.Words("EstimatedDOPIsOne"));
            Assert.AreEqual("could not generate valid parallel plan", PlanInsights.Words("CouldNotGenerateValidParallelPlan"));
            Assert.AreEqual("TSQL user defined functions not parallelizable", PlanInsights.Words("TSQLUserDefinedFunctionsNotParallelizable"));
        }

        [TestMethod]
        public void Operator_WithNothingToSay_HasNoInsights()
        {
            var statement = Parse(Op(1, "Table Scan", "Table Scan", Rows(10)));
            Assert.AreEqual(0, PlanInsights.ForOperator(statement.RootOperator!, statement).Count);
        }
    }
}
