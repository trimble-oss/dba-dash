using System;
using System.Linq;
using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Test
{
    /// <summary>The long values - predicates, defined values, output columns - laid out to be read.</summary>
    [TestClass]
    public class PlanReadingTests
    {
        private static string[] Lines(string value) =>
            PlanFormat.ForReading(value).Split(Environment.NewLine);

        [TestMethod]
        public void ForReading_PutsEachDefinedValueOnALineOfItsOwn()
        {
            var lines = Lines("[Expr1002] = CONVERT_IMPLICIT(int,[T].[a],0), [Expr1003] = isnull([T].[b],(0)), [Expr1004] = [T].[c]");

            CollectionAssert.AreEqual(
                new[] { "[Expr1002] = CONVERT_IMPLICIT(int,[T].[a],0)", "[Expr1003] = isnull([T].[b],(0))", "[Expr1004] = [T].[c]" },
                lines);
        }

        [TestMethod]
        public void ForReading_StartsALineAtEachTopLevelAndOrOr()
        {
            var lines = Lines("[T].[a]=(1) AND ([T].[b]=(2) OR [T].[c]=(3)) OR [T].[d]>(4)");

            // The OR inside the brackets belongs to the term it is in.
            CollectionAssert.AreEqual(
                new[] { "[T].[a]=(1)", "AND ([T].[b]=(2) OR [T].[c]=(3))", "OR [T].[d]>(4)" },
                lines);
        }

        [TestMethod]
        public void ForReading_LeavesCommasInNamesAndLiteralsAlone()
        {
            var lines = Lines("[T].[a, b]=N'x, AND y', [T].[c]");

            CollectionAssert.AreEqual(new[] { "[T].[a, b]=N'x, AND y'", "[T].[c]" }, lines);
        }

        [TestMethod]
        public void ForReading_LeavesCommasInsideAnEscapedApostropheAlone()
        {
            // A doubled apostrophe is an apostrophe in the string, not the end of it.  Toggling on
            // every quote is what makes that work: the pair closes and reopens with nothing between
            // the two, so no comma in the literal is ever seen at the top level.
            var lines = Lines("[T].[a]=N'O''Brien, AND sons', [T].[b]");

            CollectionAssert.AreEqual(new[] { "[T].[a]=N'O''Brien, AND sons'", "[T].[b]" }, lines);
        }

        [TestMethod]
        public void Parser_MarksPredicatesAndDefinedValuesAsExpressions()
        {
            var seek = TestPlans.Operator(TestPlans.Statement(TestPlans.KeyLookupSeek), 1);

            Assert.IsTrue(seek.Properties.Single(p => p.Name == "Seek Predicates").IsExpression);
            Assert.IsTrue(seek.Properties.Single(p => p.Name == "Defined Values").IsExpression);
            Assert.IsFalse(seek.Properties.Single(p => p.Name == "Physical Operation").IsExpression);
        }

        [TestMethod]
        public void Tooltip_ListsTheOutputColumnsLast()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(TestPlans.KeyLookupSeek));
            var tooltip = PlanTooltipBuilder.Build(layout.Nodes.Single(n => n.Operator?.NodeId == 0));

            var output = tooltip.Rows[^1];
            Assert.AreEqual("Output list", output.Label);
            Assert.AreEqual("o.OrderID, o.Total", output.Value.Replace("[", string.Empty).Replace("]", string.Empty));
            Assert.IsTrue(output.Wraps);
        }

        [TestMethod]
        public void Tooltip_WrapsTheSeekPredicate()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(TestPlans.KeyLookupSeek));
            var tooltip = PlanTooltipBuilder.Build(layout.Nodes.Single(n => n.Operator?.NodeId == 1));

            Assert.IsTrue(tooltip.Rows.Single(r => r.Label == "Seek predicate").Wraps);
            Assert.IsFalse(tooltip.Rows.Single(r => r.Label == "Estimated rows").Wraps);
        }
    }
}
