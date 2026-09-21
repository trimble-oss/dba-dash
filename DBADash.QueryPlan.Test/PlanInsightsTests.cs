using System.Linq;
using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
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

        [TestMethod]
        public void TheSameWarningOverAndOver_IsOneCard_SayingHowManyAndWhere()
        {
            // Four implicit conversions across four operators: one thing to know about the query,
            // where four cards of it would push the rest of the panel off the bottom.
            var statement = Parse(Converting(1, 1, Converting(2, 1, Converting(3, 1, Converting(4, 1)))));

            var insight = PlanInsights.ForStatement(statement)
                .Single(i => i.Text.StartsWith("Cardinality Estimate"));

            StringAssert.StartsWith(insight.Text, "Cardinality Estimate: 4 in this statement, on nodes 1, 2, 3 and 1 other.");
            StringAssert.Contains(insight.Text, "CONVERT_IMPLICIT(int,[Expr1011],0)");
            StringAssert.Contains(insight.Text, "and 2 others.", "The rest are counted rather than listed.");

            CollectionAssert.AreEqual(
                new[] { 1, 2, 3, 4 },
                insight.Operators.Select(op => op.NodeId).ToList(),
                "Every operator it covers, so the card can offer to go to each of them.");

            Assert.AreEqual(1, insight.Operator!.NodeId, "The first of them, for anything that wants one.");
        }

        [TestMethod]
        public void ACombinedWarning_CarriesEveryOneInFull_ForTheReaderToOpen()
        {
            var statement = Parse(Converting(1, 1, Converting(2, 1, Converting(3, 1, Converting(4, 1)))));

            var insight = PlanInsights.ForStatement(statement)
                .Single(i => i.Text.StartsWith("Cardinality Estimate"));

            Assert.IsTrue(insight.IsCombinedWarning, "Four of the same warning are said once, with the rest kept for opening.");
            Assert.AreEqual(4, insight.WarningCount);

            Assert.IsNotNull(insight.FullText);
            StringAssert.StartsWith(insight.FullText, "Cardinality Estimate (4)", "The list opens with the title and how many there are.");

            // Every node named, so the reader can find each one in the plan - not just the two the
            // card's summary had room for.
            foreach (var node in new[] { 1, 2, 3, 4 })
            {
                StringAssert.Contains(insight.FullText, "Node " + node + ":");
            }
        }

        [TestMethod]
        public void AWarningLeftOnItsOwnCard_HasNoFullList()
        {
            var statement = Parse(Converting(1, 1, Converting(2, 1, Converting(3, 1))));

            var insight = PlanInsights.ForStatement(statement)
                .First(i => i.Text.StartsWith("Cardinality Estimate"));

            Assert.IsFalse(insight.IsCombinedWarning, "Below the threshold each one is a card of its own, already said in full.");
            Assert.IsNull(insight.FullText);
            Assert.AreEqual(0, insight.WarningCount);
        }

        [TestMethod]
        public void AFewOfTheSameWarning_StayOneCardEach()
        {
            var statement = Parse(Converting(1, 1, Converting(2, 1, Converting(3, 1))));

            var insights = PlanInsights.ForStatement(statement)
                .Where(i => i.Text.StartsWith("Cardinality Estimate"))
                .ToList();

            Assert.AreEqual(3, insights.Count, "Below the threshold each one is worth going to look at.");
            Assert.IsTrue(insights.All(i => i.Operators.Count == 1));
        }

        [TestMethod]
        public void AnOperatorWithTheSameWarningOverAndOver_AlsoGetsOneCard()
        {
            var statement = Parse(Converting(1, 5));

            var insight = PlanInsights.ForOperator(statement.RootOperator!, statement).Single();

            StringAssert.StartsWith(insight.Text, "Cardinality Estimate: 5 on this operator.");
        }

        [TestMethod]
        public void TheTooltipCombinesThemToo_RatherThanARowEach()
        {
            var statement = Parse(Converting(1, 5));
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(statement);

            var tooltip = PlanTooltipBuilder.Build(layout.Nodes.Single(n => n.Operator?.NodeId == 1));
            var row = tooltip.Rows.Single(r => r.Label == "Cardinality Estimate");

            StringAssert.StartsWith(row.Value, "5 of them: CONVERT_IMPLICIT(int,[Expr1011],0)");
            StringAssert.Contains(row.Value, "and 3 others");
        }

        [TestMethod]
        public void AConversionReportedAgainstTheStatement_IsTracedToTheOperatorThatEvaluatesIt()
        {
            // Showplan reports a plan affecting convert against the statement, not against an
            // operator - so without tracing it the picture marks the SELECT and nothing else.
            var statement = Parse(
                Converts("CONVERT_IMPLICIT(int,[db].[dbo].[T].[Reference],0)=[@p1]") +
                Op(0, "Filter", "Filter", Rows(1),
                    Op(1, "Index Scan", "Index Scan", Rows(10),
                        Predicate("CONVERT_IMPLICIT(int,[db].[dbo].[T].[Reference],0)=[@p1]"))));

            var warning = statement.Warnings.Single();
            var scan = TestPlans.Operator(statement, 1);

            CollectionAssert.AreEqual(new[] { 1 }, warning.Operators.Select(op => op.NodeId).ToList());
            Assert.AreEqual("Index Scan (node 1)", warning.OperatorsDescription);
            CollectionAssert.AreEqual(new[] { warning }, statement.WarningsFor(scan).ToList());

            // The node carries the marker, so the operator doing the conversion is the one the
            // picture points at.
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(statement);
            var node = layout.Nodes.Single(n => n.Operator?.NodeId == 1);
            Assert.IsTrue(node.Badges.HasFlag(PlanNodeBadges.Warning));
            Assert.IsFalse(layout.Nodes.Single(n => n.Operator?.NodeId == 0).Badges.HasFlag(PlanNodeBadges.Warning));

            // And it is on that operator's own cards, for the reader who selected it after seeing it.
            StringAssert.StartsWith(PlanInsights.ForOperator(scan, statement).Single().Text, "Cardinality Estimate");

            // The statement's card offers to go there.
            var insight = PlanInsights.ForStatement(statement).Single(i => i.Text.StartsWith("Cardinality Estimate"));
            Assert.AreEqual(1, insight.Operator!.NodeId);
        }

        [TestMethod]
        public void AConversionComparedOneWayAndEvaluatedAnother_IsStillTraced()
        {
            // The warning names the comparison the conversion took part in; the operator turned that
            // comparison into a range.  The conversion itself is the part they share.
            var statement = Parse(
                Converts("CONVERT_IMPLICIT(int,[db].[dbo].[T].[Reference],0)=[@p1]") +
                Op(0, "Index Seek", "Index Seek", Rows(10),
                    Predicate("CONVERT_IMPLICIT(int,[db].[dbo].[T].[Reference],0)>=[@p1] AND " +
                              "CONVERT_IMPLICIT(int,[db].[dbo].[T].[Reference],0)&lt;=[@p2]")));

            CollectionAssert.AreEqual(new[] { 0 }, statement.Warnings.Single().Operators.Select(op => op.NodeId).ToList());
        }

        [TestMethod]
        public void AConversionNothingInThePlanMatches_IsLeftWhereShowplanPutIt()
        {
            var statement = Parse(
                Converts("CONVERT_IMPLICIT(int,[db].[dbo].[Other].[Column],0)=[@p1]") +
                Op(0, "Table Scan", "Table Scan", Rows(10)));

            Assert.AreEqual(0, statement.Warnings.Single().Operators.Count);

            var insight = PlanInsights.ForStatement(statement).Single(i => i.Text.StartsWith("Cardinality Estimate"));
            Assert.IsNull(insight.Operator, "Nowhere to send the reader beats sending them to a guess.");
        }

        [TestMethod]
        public void ManyConversionsAgainstTheStatement_AreAlsoOneCard_NamingTheNodesTheyHappenIn()
        {
            var statement = Parse(
                Converts(Enumerable.Range(1, 4)
                    .Select(n => $"CONVERT_IMPLICIT(int,[db].[dbo].[T].[c{n}],0)=[@p{n}]").ToArray()) +
                Op(0, "Filter", "Filter", Rows(1),
                    Predicate("CONVERT_IMPLICIT(int,[db].[dbo].[T].[c1],0)=[@p1] AND " +
                              "CONVERT_IMPLICIT(int,[db].[dbo].[T].[c2],0)=[@p2]"),
                    Op(1, "Index Scan", "Index Scan", Rows(10),
                        Predicate("CONVERT_IMPLICIT(int,[db].[dbo].[T].[c3],0)=[@p3] AND " +
                                  "CONVERT_IMPLICIT(int,[db].[dbo].[T].[c4],0)=[@p4]"))));

            var insight = PlanInsights.ForStatement(statement).Single(i => i.Text.StartsWith("Cardinality Estimate"));

            StringAssert.StartsWith(insight.Text, "Cardinality Estimate: 4 in this statement, on nodes 0 and 1.");
            CollectionAssert.AreEqual(new[] { 0, 1 }, insight.Operators.Select(op => op.NodeId).ToList());
        }

        /// <summary>
        /// The statement's warnings, as showplan reports plan affecting conversions: one Warnings
        /// element holding one entry per conversion.
        /// </summary>
        private static string Converts(params string[] expressions) =>
            "<Warnings>" +
            string.Concat(expressions.Select(expression =>
                $"""<PlanAffectingConvert ConvertIssue="Cardinality Estimate" Expression="{expression}" />""")) +
            "</Warnings>";

        /// <summary>A residual predicate, which is where a conversion shows up in an operator.</summary>
        private static string Predicate(string expression) =>
            $"""<Predicate><ScalarOperator ScalarString="{expression}" /></Predicate>""";

        /// <summary>
        /// An operator carrying <paramref name="count"/> plan affecting convert warnings, which is
        /// what a query comparing a varchar column against integers reports one of per comparison.
        /// </summary>
        private static string Converting(int id, int count, params string[] children)
        {
            var warnings = string.Concat(Enumerable.Range(1, count).Select(n =>
                $"""<PlanAffectingConvert ConvertIssue="Cardinality Estimate" Expression="CONVERT_IMPLICIT(int,[Expr10{id}{n}],0)" />"""));

            return $"""
                    <RelOp NodeId="{id}" PhysicalOp="Table Scan" LogicalOp="Table Scan" EstimateRows="1" AvgRowSize="9" EstimatedTotalSubtreeCost="1">
                      <Warnings>{warnings}</Warnings>
                      <Body>{string.Concat(children)}</Body>
                    </RelOp>
                    """;
        }
    }
}
