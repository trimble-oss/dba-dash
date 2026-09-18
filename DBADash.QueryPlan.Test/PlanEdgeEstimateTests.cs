using System.Linq;
using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DBADash.QueryPlan.Test.InlinePlan;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// Arrows drawn with their estimate over them, coloured by how far out it was, and the tooltip
    /// that shows both figures.
    /// </summary>
    [TestClass]
    public class PlanEdgeEstimateTests
    {
        private static PlanLayout Layout(PlanStatement statement, PlanEdgeWidthBasis basis = PlanEdgeWidthBasis.Actual) =>
            new PlanLayoutEngine(new FakeTextMeasurer(), new PlanLayoutOptions { EdgeWidthBasis = basis }).Layout(statement);

        private static PlanLayout Layout(string plan, PlanEdgeWidthBasis basis = PlanEdgeWidthBasis.Actual) =>
            Layout(TestPlans.Statement(plan), basis);

        private static PlanEdge EdgeFrom(PlanLayout layout, int nodeId) =>
            layout.Edges.Single(e => e.From.Operator?.NodeId == nodeId);

        [TestMethod]
        public void Edges_KnowBothFiguresWhateverTheyAreDrawnBy()
        {
            // Drawn by actual rows, but the estimate is still on the arrow for the tooltip.
            var scan = EdgeFrom(Layout(TestPlans.ParallelSpill), 3);

            Assert.AreEqual(10_000, scan.ActualRows);
            Assert.AreEqual(100, scan.EstimatedRows);
            Assert.AreEqual(30, scan.RowSize);
            Assert.AreEqual(300_000, scan.ActualDataSize);
            Assert.AreEqual(3_000, scan.EstimatedDataSize);
            Assert.AreEqual(100, scan.EstimateError);
            Assert.AreEqual(PlanEstimateAccuracy.Critical, scan.EstimateAccuracy);

            // No outline unless both are asked for.
            Assert.IsNull(scan.EstimateThickness);
        }

        [TestMethod]
        public void Both_OutlinesTheEstimateOnTheSameScaleAsTheArrow()
        {
            var layout = Layout(TestPlans.ParallelSpill, PlanEdgeWidthBasis.Both);
            Assert.AreEqual(PlanEdgeWidthBasis.Both, layout.EffectiveEdgeWidthBasis);

            // Underestimated 100 times: a fat arrow with a thin outline inside it.
            var scan = EdgeFrom(layout, 3);
            Assert.IsTrue(scan.EstimateThickness < scan.Thickness);
            Assert.IsTrue(scan.IsActual, "The body is still the actual rows.");
            Assert.AreEqual("10K", scan.Label, "The label is unchanged - the tooltip carries the estimate.");

            // Estimated exactly: the outline sits on the arrow's edges.
            var sort = EdgeFrom(layout, 1);
            Assert.AreEqual(sort.Thickness, sort.EstimateThickness!.Value, 0.0001);
            Assert.AreEqual(PlanEstimateAccuracy.Good, sort.EstimateAccuracy);
        }

        [TestMethod]
        public void Both_FallsBackToTheEstimatesOnAnEstimatedPlan()
        {
            var layout = Layout(TestPlans.KeyLookupSeek, PlanEdgeWidthBasis.Both);

            // Nothing was measured, so there is nothing to outline and nothing to colour.
            Assert.AreEqual(PlanEdgeWidthBasis.Estimated, layout.EffectiveEdgeWidthBasis);
            Assert.IsTrue(layout.Edges.All(e => e.EstimateThickness is null && e.EstimateAccuracy is null && e.ActualRows is null));
        }

        [TestMethod]
        public void Accuracy_IsBandedAtTenAndAHundredTimesEitherWay()
        {
            var layout = Layout(Parse(
                Op(0, "Concatenation", "Concatenation", Rows(53_501),
                    Op(1, "Index Scan", "Index Scan", 100, Rows(500)),
                    Op(2, "Index Scan", "Index Scan", 100, Rows(1_000)),
                    Op(3, "Index Scan", "Index Scan", 100, Rows(2_000)),
                    Op(4, "Index Scan", "Index Scan", 100, Rows(10_000)),
                    Op(5, "Index Scan", "Index Scan", 50, Rows(1)))));

            Assert.AreEqual(PlanEstimateAccuracy.Good, EdgeFrom(layout, 1).EstimateAccuracy, "5 times out.");
            Assert.AreEqual(PlanEstimateAccuracy.Warning, EdgeFrom(layout, 2).EstimateAccuracy, "Exactly 10 times out.");
            Assert.AreEqual(PlanEstimateAccuracy.Warning, EdgeFrom(layout, 3).EstimateAccuracy, "20 times out.");
            Assert.AreEqual(PlanEstimateAccuracy.Critical, EdgeFrom(layout, 4).EstimateAccuracy, "Exactly 100 times out.");
            Assert.AreEqual(PlanEstimateAccuracy.Warning, EdgeFrom(layout, 5).EstimateAccuracy, "50 times over - either way counts.");
        }

        [TestMethod]
        public void Accuracy_TakesCountsBelowOneRowAsOne()
        {
            // An estimate of 0.3 rows against the one that came back is as good as an estimate gets.
            var layout = Layout(Parse(
                Op(0, "Top", "Top", Rows(1),
                    Op(1, "Index Seek", "Index Seek", 0.3, Rows(1)))));

            Assert.AreEqual(1, EdgeFrom(layout, 1).EstimateError);
            Assert.AreEqual(PlanEstimateAccuracy.Good, EdgeFrom(layout, 1).EstimateAccuracy);
        }

        [TestMethod]
        public void Tooltip_ShowsBothFiguresInRowsAndBytes()
        {
            var tooltip = PlanTooltipBuilder.BuildForEdge(EdgeFrom(Layout(TestPlans.ParallelSpill), 3));

            Assert.IsTrue(tooltip.Title.Contains('→'), "Says which arrow it is: producer to consumer.");
            Assert.AreEqual("10,000", Value(tooltip, "Actual rows"));
            Assert.AreEqual("100", Value(tooltip, "Estimated rows"));
            Assert.AreEqual("100x more", Value(tooltip, "Actual vs estimated"));
            Assert.IsTrue(tooltip.Rows.Single(r => r.Label == "Actual vs estimated").IsEmphasised);
            Assert.AreEqual("30 B (estimated)", Value(tooltip, "Row size"));
            Assert.AreEqual("293 KB", Value(tooltip, "Actual data size"));
            Assert.AreEqual("2.9 KB", Value(tooltip, "Estimated data size"));
        }

        [TestMethod]
        public void Tooltip_OnAnEstimatedPlanShowsWhereTheTotalCameFrom()
        {
            var tooltip = PlanTooltipBuilder.BuildForEdge(EdgeFrom(Layout(TestPlans.KeyLookupSeek), 2));

            // One row a time, ten times over - and nothing actual to show.
            Assert.AreEqual("10  (1 × 10 executions)", Value(tooltip, "Estimated rows"));
            Assert.IsFalse(tooltip.Rows.Any(r => r.Label.StartsWith("Actual", System.StringComparison.Ordinal)));
        }

        [TestMethod]
        public void EdgeAt_FindsAnArrowFromAnywhereAlongItsWidth()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);
            var lookup = EdgeFrom(layout, 2);
            var start = lookup.Points[0];
            var corner = lookup.Points[1];
            var middle = new LayoutPoint((start.X + corner.X) / 2, start.Y);

            Assert.AreSame(lookup, layout.EdgeAt(middle));

            // Beyond its half width, only the tolerance reaches it.
            var beside = new LayoutPoint(middle.X, middle.Y + (lookup.Thickness / 2) + 3);
            Assert.IsNull(layout.EdgeAt(beside));
            Assert.AreSame(lookup, layout.EdgeAt(beside, tolerance: 4));
        }

        [TestMethod]
        public void Hover_OnAnArrowShowsItsTooltipBesideThePointer()
        {
            var controller = new PlanViewController(Layout(TestPlans.ParallelSpill));
            controller.SetViewport(new LayoutSize(800, 600));

            var scan = EdgeFrom(controller.Layout, 3);
            var middle = new LayoutPoint((scan.Points[0].X + scan.Points[1].X) / 2, scan.Points[0].Y);
            var screen = controller.ToScreen(middle);

            Assert.IsTrue(controller.SetHover(screen));
            Assert.AreSame(scan, controller.HoveredEdge);
            Assert.IsNull(controller.HoveredNode);
            Assert.AreEqual("10,000", Value(controller.HoveredTooltip!, "Actual rows"));
            Assert.AreEqual(middle.X, controller.HoverAnchor!.Value.X, 0.001);

            // Moving along the same arrow is not a change worth repainting for.
            Assert.IsFalse(controller.SetHover(screen));

            // Routing the arrows again replaces them, so the hovered one is forgotten.
            controller.EdgeWidthBasis = PlanEdgeWidthBasis.Both;
            Assert.IsNull(controller.HoveredEdge);
            Assert.IsNull(controller.HoveredTooltip);
        }

        [TestMethod]
        public void Hover_PrefersTheNodeWhereAnArrowMeetsIt()
        {
            var controller = new PlanViewController(Layout(TestPlans.ParallelSpill));
            controller.SetViewport(new LayoutSize(800, 600));

            // The arrow's first point is on the producer's left edge, inside the node.
            var scan = EdgeFrom(controller.Layout, 3);
            var onNode = new LayoutPoint(scan.Points[0].X + 2, scan.Points[0].Y);

            controller.SetHover(controller.ToScreen(onNode));

            Assert.AreEqual(3, controller.HoveredNode!.Operator!.NodeId);
            Assert.IsNull(controller.HoveredEdge);
        }

        // ---------------------------------------------------------------- unmeasured Compute Scalars

        /// <summary>
        /// Two Compute Scalars SQL Server did not measure, between an aggregate and the scan that
        /// read 500 rows against an estimate of 5.
        /// </summary>
        private static PlanStatement UnmeasuredComputeScalars() => Parse(
            Op(0, "Hash Match", "Aggregate", 5, Rows(1),
                Op(1, "Compute Scalar", "Compute Scalar", 5, "",
                    Op(2, "Compute Scalar", "Compute Scalar", 5, "",
                        Op(3, "Index Scan", "Index Scan", 5, Rows(500))))));

        [TestMethod]
        public void ActualRows_OfAnUnmeasuredComputeScalarAreItsInputs()
        {
            var statement = UnmeasuredComputeScalars();

            // Followed down the chain to the first operator that measured anything.
            var outer = TestPlans.Operator(statement, 1);
            Assert.IsNull(outer.Runtime);
            Assert.AreEqual(500, outer.ActualRows);
            Assert.IsTrue(outer.IsActualRowsInferred);

            var scan = TestPlans.Operator(statement, 3);
            Assert.AreEqual(500, scan.ActualRows);
            Assert.IsFalse(scan.IsActualRowsInferred, "Measured, not inferred.");
        }

        [TestMethod]
        public void ActualRows_AreNotInferredForAnythingThatCanChangeTheCount()
        {
            // A filter with no counters could have kept any number of its input's rows.
            var statement = Parse(
                Op(0, "Top", "Top", 1, Rows(1),
                    Op(1, "Filter", "Filter", 1, "",
                        Op(2, "Index Scan", "Index Scan", 1, Rows(500)))));

            Assert.IsNull(TestPlans.Operator(statement, 1).ActualRows);
        }

        [TestMethod]
        public void Edges_OutOfAnUnmeasuredComputeScalarAreDrawnAndColouredByTheInferredRows()
        {
            var layout = Layout(UnmeasuredComputeScalars(), PlanEdgeWidthBasis.Both);
            var edge = EdgeFrom(layout, 1);

            Assert.AreEqual(500, edge.ActualRows);
            Assert.IsTrue(edge.IsActualRowsInferred);
            Assert.IsTrue(edge.IsActual, "Drawn in the actual colour.");
            Assert.IsNotNull(edge.EstimateThickness, "Outlined, now there is an actual to compare with.");
            Assert.AreEqual(PlanEstimateAccuracy.Critical, edge.EstimateAccuracy, "500 against 5.");

            // The node says the same count as the arrow.
            StringAssert.Contains(layout.Nodes.Single(n => n.Operator?.NodeId == 1).MetricLine, "500 rows");

            // And the tooltips say where the figure came from.
            Assert.AreEqual("500  (from its input, not measured)", Value(PlanTooltipBuilder.BuildForEdge(edge), "Actual rows"));
            var node = PlanTooltipBuilder.Build(layout.Nodes.Single(n => n.Operator?.NodeId == 1));
            Assert.AreEqual("500  (from its input, not measured)", Value(node, "Actual rows"));
        }

        [TestMethod]
        public void Properties_OfAnUnmeasuredComputeScalarShowTheInferredRows()
        {
            var properties = TestPlans.Operator(UnmeasuredComputeScalars(), 1).Properties;

            Assert.AreEqual("500 (from its input, not measured)", properties.Single(p => p.Name == "Actual Rows").Value);
        }

        [TestMethod]
        public void Statement_WithAnUnmeasuredComputeScalarAtTheRootIsStillAnActualPlan()
        {
            // The root is often a Compute Scalar; asking only the root called these estimated plans.
            var plan = ParsePlan(
                Op(0, "Compute Scalar", "Compute Scalar", 1, "",
                    Op(1, "Stream Aggregate", "Aggregate", 1, Rows(3))));

            Assert.IsTrue(plan.Statements[0].IsActualPlan);
            Assert.AreEqual(3, PlanStatementSummary.For(plan).Single().ActualRows);
        }

        private static string Value(PlanTooltip tooltip, string label) =>
            tooltip.Rows.Single(r => r.Label == label).Value;
    }
}
