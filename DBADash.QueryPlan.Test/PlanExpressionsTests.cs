using System.Linq;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DBADash.QueryPlan.Test.InlinePlan;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// The named values a plan works out for itself: which ones are collected, what they expand to
    /// once the names they are built from are written out, and which operators use them.
    /// </summary>
    [TestClass]
    public class PlanExpressionsTests
    {
        /// <summary>A DefinedValues element naming <paramref name="name"/> as <paramref name="expression"/>.</summary>
        private static string Defines(string name, string expression) =>
            $"""
             <DefinedValues>
               <DefinedValue>
                 <ColumnReference Column="{name}" />
                 <ScalarOperator ScalarString="{expression}" />
               </DefinedValue>
             </DefinedValues>
             """;

        [TestMethod]
        public void TheValuesThePlanWorksOut_AreCollectedWithTheOperatorThatWorksThemOut()
        {
            var statement = Parse(Op(1, "Compute Scalar", "Compute Scalar", "",
                Defines("Expr1002", "[dbo].[T].[a]+(1)"),
                Op(2, "Table Scan", "Table Scan", Rows(10))));

            var expression = statement.Expressions.Single();

            Assert.AreEqual("Expr1002", expression.Name);
            Assert.AreEqual("[dbo].[T].[a]+(1)", expression.Definition);
            Assert.AreEqual(1, expression.DefinedBy!.NodeId);
            Assert.IsTrue(expression.IsGenerated);
            Assert.IsFalse(expression.IsNested, "It refers to nothing, so there is nothing to write out.");
            Assert.AreSame(expression, statement.ExpressionNamed("Expr1002"));
        }

        [TestMethod]
        public void AColumnPassedThroughUnchanged_IsNotSomethingThePlanWorkedOut()
        {
            // A DefinedValue with no expression behind it is showplan saying the operator hands the
            // column on, which is not a value the plan computed.
            var statement = Parse(Op(1, "Index Scan", "Index Scan", Rows(10), """
                <DefinedValues>
                  <DefinedValue>
                    <ColumnReference Database="[db]" Schema="[dbo]" Table="[T]" Column="a" />
                  </DefinedValue>
                </DefinedValues>
                """));

            Assert.AreEqual(0, statement.Expressions.Count);
        }

        [TestMethod]
        public void AnExpressionBuiltFromOthers_ExpandsToTheWholeThing()
        {
            var statement = Parse(Op(1, "Compute Scalar", "Compute Scalar", "",
                Defines("Expr1003", "CONVERT_IMPLICIT(int,[Expr1002],0)"),
                Op(2, "Compute Scalar", "Compute Scalar", "",
                    Defines("Expr1002", "[dbo].[T].[a]+[Expr1001]"),
                    Op(3, "Compute Scalar", "Compute Scalar", "",
                        Defines("Expr1001", "len([dbo].[T].[b])"),
                        Op(4, "Table Scan", "Table Scan", Rows(10))))));

            var outermost = statement.ExpressionNamed("Expr1003")!;

            CollectionAssert.AreEqual(new[] { "Expr1002" }, outermost.References.ToList());
            Assert.IsTrue(outermost.IsNested);

            // Bracketed as it is substituted, because what was one operand becomes a whole expression.
            Assert.AreEqual(
                "CONVERT_IMPLICIT(int,([dbo].[T].[a]+(len([dbo].[T].[b]))),0)",
                outermost.Expanded);

            Assert.AreEqual("len([dbo].[T].[b])", statement.ExpressionNamed("Expr1001")!.Expanded,
                "One that refers to nothing expands to itself.");
        }

        [TestMethod]
        public void ExpandedReferencesIn_WritesEveryNamedValueOutInPlace()
        {
            var statement = Parse(Op(1, "Compute Scalar", "Compute Scalar", "",
                Defines("Expr1002", "[dbo].[T].[a]+(1)"),
                Op(2, "Table Scan", "Table Scan", Rows(10))));

            // A warning's detail names the value the plan invented; expanding it writes the value out
            // where the name was, bracketed.
            Assert.AreEqual(
                "CONVERT_IMPLICIT(int,([dbo].[T].[a]+(1)),0)=[@Reference]",
                PlanExpressions.ExpandedReferencesIn(statement, "CONVERT_IMPLICIT(int,[Expr1002],0)=[@Reference]"));
        }

        [TestMethod]
        public void ExpandedReferencesIn_IsEmptyWhenTheTextNamesNothingThePlanDefines()
        {
            var statement = Parse(Op(1, "Compute Scalar", "Compute Scalar", "",
                Defines("Expr1002", "[dbo].[T].[a]+(1)"),
                Op(2, "Table Scan", "Table Scan", Rows(10))));

            Assert.AreEqual(string.Empty, PlanExpressions.ExpandedReferencesIn(statement, "[dbo].[T].[a] > (5)"),
                "Nothing to add, so a caller can leave the cell empty rather than repeat the text.");
        }

        [TestMethod]
        public void AParallelAggregateDefiningTheSameNameAgain_StillResolvesToWhatWorksTheValueOut()
        {
            // A parallel aggregate computes a value in a Compute Scalar and carries it up through a
            // local and a global Stream Aggregate, each defining the same name again as ANY of
            // itself.  Showplan really does hold the name three times; what it means is the one that
            // works it out.
            var statement = Parse(Op(1, "Stream Aggregate", "Aggregate", "",
                Defines("Expr1033", "ANY([Expr1033])"),
                Op(2, "Stream Aggregate", "Aggregate", "",
                    Defines("Expr1033", "ANY([Expr1033])"),
                    Op(3, "Compute Scalar", "Compute Scalar", "",
                        Defines("Expr1033", "CASE WHEN [t].[a] IS NOT NULL THEN [t].[a] ELSE [t].[b] END"),
                        Op(4, "Table Scan", "Table Scan", Rows(10))))));

            Assert.AreEqual(3, statement.Expressions.Count, "Each definition is something an operator does.");

            var resolved = statement.ExpressionNamed("Expr1033")!;
            Assert.AreEqual(3, resolved.DefinedBy!.NodeId);
            StringAssert.StartsWith(resolved.Definition, "CASE WHEN");

            // And the ones that only carry it say so, rather than answering the question with itself.
            foreach (var carried in statement.Expressions.Where(e => e.Definition.StartsWith("ANY")))
            {
                Assert.AreEqual("ANY((CASE WHEN [t].[a] IS NOT NULL THEN [t].[a] ELSE [t].[b] END))", carried.Expanded);
            }
        }

        [TestMethod]
        public void AnExpressionThatRefersBackToItself_IsLeftAsAName_RatherThanExpandingForever()
        {
            var statement = Parse(Op(1, "Compute Scalar", "Compute Scalar", "",
                Defines("Expr1001", "[Expr1002]+(1)"),
                Op(2, "Compute Scalar", "Compute Scalar", "",
                    Defines("Expr1002", "[Expr1001]+(2)"),
                    Op(3, "Table Scan", "Table Scan", Rows(10)))));

            var first = statement.ExpressionNamed("Expr1001")!;

            Assert.AreEqual("([Expr1001]+(2))+(1)", first.Expanded);
        }

        [TestMethod]
        public void WhereAnExpressionIsUsed_IsRecorded_AndTheOperatorDefiningItIsNotAUseOfIt()
        {
            var statement = Parse(Op(1, "Filter", "Filter", Rows(1), """
                <Predicate><ScalarOperator ScalarString="[Expr1002]&gt;(10)" /></Predicate>
                """,
                Op(2, "Compute Scalar", "Compute Scalar", "",
                    Defines("Expr1002", "[dbo].[T].[a]+(1)"),
                    Op(3, "Table Scan", "Table Scan", Rows(10)))));

            var expression = statement.ExpressionNamed("Expr1002")!;

            Assert.AreEqual(2, expression.DefinedBy!.NodeId);
            CollectionAssert.AreEqual(new[] { 1 }, expression.UsedBy.Select(op => op.NodeId).ToList());
        }

        [TestMethod]
        public void OnARealPlan_TheListHoldsEveryComputedValue_WithWhereItComesFromAndWhereItGoes()
        {
            var statement = TestPlans.Statement(TestPlans.Expressions);

            CollectionAssert.AreEqual(
                new[] { "Expr1004", "Expr1003", "Expr1002", "Expr1001" },
                statement.Expressions.Select(e => e.Name).ToList(),
                "The order the plan works them out in, outermost operator first.  The scan's " +
                "pass-through columns are not values the plan computed.");

            var outermost = statement.ExpressionNamed("Expr1004")!;

            Assert.AreEqual("Compute Scalar (node 1)", outermost.DefinedByDescription);
            Assert.AreEqual("Filter (node 0)", outermost.UsedByDescription);
            Assert.AreEqual(
                "CONVERT_IMPLICIT(int,(len([Sales].[dbo].[Orders].[Reference] as [o].[Reference])+" +
                "([Sales].[dbo].[Orders].[Total] as [o].[Total]*(1.2))),0)+" +
                "([Sales].[dbo].[Orders].[Total] as [o].[Total]*(1.2))",
                outermost.Expanded,
                "Written out through both levels, each substitution bracketed.");

            Assert.AreEqual(string.Empty, statement.ExpressionNamed("Expr1002")!.ExpandedOneLine,
                "A value built from nothing else has no expansion worth a column.");
        }

        [TestMethod]
        public void OnARealPlan_TheRepeatedConversionWarning_IsOneCardNamingTheExpressionsItIsAbout()
        {
            var statement = TestPlans.Statement(TestPlans.Expressions);

            var insight = PlanInsights.ForStatement(statement)
                .Single(i => i.Text.StartsWith("Cardinality Estimate"));

            StringAssert.StartsWith(insight.Text, "Cardinality Estimate: 5 in this statement, on node 0.");
            StringAssert.Contains(insight.Text, "[Expr1001]", "The names are in the text for the card to link.");

            // And the names it carries are ones the statement can say the meaning of.
            Assert.IsNotNull(statement.ExpressionNamed(
                PlanExpressions.ReferencesIn(insight.Text).First().Name));
        }

        [TestMethod]
        public void TextThatRefersToThePlansOwnValues_CanBeShownWithThemWrittenUnderneath()
        {
            var statement = TestPlans.Statement(TestPlans.Expressions);
            var predicate = statement.Operators.First(op => op.Predicate is not null).Predicate!;

            var notes = PlanScripts.ExpressionsUsedIn(statement, predicate);

            StringAssert.Contains(notes, "Expr1001 = substring([Sales].[dbo].[Orders].[Reference]");

            // A sort key or a group by writes the name bare, and is read as often as a predicate.
            StringAssert.Contains(
                PlanScripts.ExpressionsUsedIn(statement, "Expr1002 ASC"),
                "Expr1002 = [Sales].[dbo].[Orders].[Total]");
            StringAssert.Contains(notes, "Expr1004 = CONVERT_IMPLICIT(int,[Expr1003],0)+[Expr1002]");
            StringAssert.Contains(notes, "in full:", "The one built from others is also written out.");
            StringAssert.StartsWith(notes.TrimStart(), "/*", "A block comment, so it can sit under the value in the code viewer.");

            Assert.AreEqual(string.Empty, PlanScripts.ExpressionsUsedIn(statement, "[Sales].[dbo].[Orders].[Total]>(100)"),
                "Nothing is added to text that refers to none of them.");
        }

        [TestMethod]
        public void OneExpressionWrittenOut_SaysWhereItComesFromAndWhereItGoes()
        {
            var statement = TestPlans.Statement(TestPlans.Expressions);

            var script = PlanScripts.Expression(statement.ExpressionNamed("Expr1004")!);

            StringAssert.Contains(script, "/* Expr1004");
            StringAssert.Contains(script, "Worked out by Compute Scalar (node 1)");
            StringAssert.Contains(script, "Used by Filter (node 0)");
            StringAssert.Contains(script, "Built from Expr1003, Expr1002");
            StringAssert.Contains(script, "written out in place");
        }

        [TestMethod]
        public void TheOutputList_SaysWhatEachOfThePlansOwnNamesMeans()
        {
            var statement = TestPlans.Statement(TestPlans.Expressions);
            var computeScalar = TestPlans.Operator(statement, 1);

            var output = computeScalar.Properties.Single(p => p.Name == "Output List");
            var expression = output.Children.Single(c => c.Name == "Expr1004");
            var column = output.Children.Single(c => c.Name == "o.OrderID");

            Assert.AreEqual("CONVERT_IMPLICIT(int,[Expr1003],0)+[Expr1002]", expression.Value,
                "A generated name says nothing on its own, and this is where a reader meets one.");
            Assert.IsTrue(expression.IsExpression, "So it is offered laid out for reading, like a predicate.");

            Assert.IsNull(column.Value, "A column of a table already says what it is.");
        }

        [TestMethod]
        public void AnOutputListNameCarriedUpByAnAggregate_ShowsWhatThatOperatorDoesWithIt()
        {
            // The aggregate outputs Expr1033 and defines it as ANY of itself; the Compute Scalar
            // below works the value out.  Each says what it does with the name, rather than both
            // showing the same text.
            var statement = Parse(
                Defining(1, "Stream Aggregate", "Aggregate", "Expr1033", "ANY([Expr1033])",
                    Defining(2, "Compute Scalar", "Compute Scalar", "Expr1033", "[t].[a]+[t].[b]",
                        Op(3, "Table Scan", "Table Scan", Rows(10)))));

            Assert.AreEqual("ANY([Expr1033])", OutputValue(TestPlans.Operator(statement, 1), "Expr1033"));
            Assert.AreEqual("[t].[a]+[t].[b]", OutputValue(TestPlans.Operator(statement, 2), "Expr1033"));
        }

        private static string? OutputValue(PlanOperator op, string column) =>
            op.Properties.Single(p => p.Name == "Output List").Children.Single(c => c.Name == column).Value;

        /// <summary>
        /// An operator that outputs one of the plan's own names and defines it.  Written out here
        /// because an output list is a child of the RelOp rather than of its body.
        /// </summary>
        private static string Defining(int id, string physicalOp, string logicalOp, string name, string definition, params string[] children) =>
            $"""
             <RelOp NodeId="{id}" PhysicalOp="{physicalOp}" LogicalOp="{logicalOp}" EstimateRows="1" AvgRowSize="9" EstimatedTotalSubtreeCost="1">
               <OutputList><ColumnReference Column="{name}" /></OutputList>
               <Body>{Defines(name, definition)}{string.Concat(children)}</Body>
             </RelOp>
             """;

        [TestMethod]
        public void OnlyGeneratedNames_AreTakenAsReferences()
        {
            Assert.IsTrue(PlanExpressions.IsGeneratedName("Expr1011"));
            Assert.IsTrue(PlanExpressions.IsGeneratedName("ConstExpr1005"));
            Assert.IsFalse(PlanExpressions.IsGeneratedName("OrderID"), "A column named like a column is not the plan's own.");
            Assert.IsFalse(PlanExpressions.IsGeneratedName(null));

            CollectionAssert.AreEqual(
                new[] { "Expr1011" },
                PlanExpressions.ReferencesIn("CONVERT_IMPLICIT(int,[Expr1011],0)=[dbo].[T].[Total]")
                    .Select(reference => reference.Name).ToList());
        }

        [TestMethod]
        public void AValueWrittenToAColumn_KeepsTheColumnItBelongsTo_AndIsNotLinkedAsAPlanName()
        {
            var statement = Parse(Op(1, "Clustered Index Update", "Update", Rows(1), """
                <DefinedValues>
                  <DefinedValue>
                    <ColumnReference Database="[db]" Schema="[dbo]" Table="[T]" Column="Total" />
                    <ScalarOperator ScalarString="(0)" />
                  </DefinedValue>
                </DefinedValues>
                """));

            var expression = statement.Expressions.Single();

            Assert.AreEqual("T.Total", expression.DisplayName);
            Assert.IsFalse(expression.IsGenerated);
            Assert.IsNull(statement.ExpressionNamed("Total"), "Only the plan's own names are looked up.");
        }
    }
}
