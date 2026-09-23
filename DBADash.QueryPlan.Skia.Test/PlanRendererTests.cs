using System;
using System.Linq;
using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia.Test
{
    [TestClass]
    public class PlanRendererTests
    {
        [TestMethod]
        public void Render_DrawsThePlan()
        {
            using var fixture = new RenderFixture();
            fixture.Render();

            Assert.IsTrue(fixture.DrawnPixelCount() > 200, "Nothing much was drawn.");
        }

        [TestMethod]
        public void Render_ClearsToTheBackgroundColour()
        {
            using var fixture = new RenderFixture();
            fixture.Render();

            using var bitmap = fixture.Snapshot();

            // The top left corner is margin on any plan, so it is background.
            Assert.AreEqual(fixture.Renderer.Palette.Background, bitmap.GetPixel(1, 1));
        }

        /// <summary>
        /// The sample's scan was estimated at 100 rows and read 10,000: a hundred times out.
        /// </summary>
        private static PlanEdge WorstEstimate(RenderFixture fixture) =>
            fixture.Layout.Edges.First(e => e.EstimateAccuracy == PlanEstimateAccuracy.Critical);

        [TestMethod]
        public void Render_ColoursAnArrowByHowFarItsEstimateWasOut()
        {
            using var fixture = new RenderFixture();
            fixture.Render();

            // Counted inside the arrow's own footprint, so the spill's warning marker elsewhere on
            // the plan cannot stand in for the colouring being tested.
            var edge = WorstEstimate(fixture);
            var critical = fixture.PixelsNearEdge(edge, fixture.Renderer.Palette.Critical);
            var plain = fixture.PixelsNearEdge(edge, fixture.Renderer.Palette.EdgeActual);

            Assert.IsTrue(critical > plain,
                $"Expected the arrow to be drawn in the critical colour: {critical} critical pixels, {plain} plain.");
        }

        [TestMethod]
        public void Render_LeavesAGoodEstimatesArrowTheActualColour()
        {
            using var fixture = new RenderFixture();
            fixture.Render();

            // Colouring the good ones too would leave nothing for the eye to land on.
            var edge = fixture.Layout.Edges.First(e => e.EstimateAccuracy == PlanEstimateAccuracy.Good);
            var critical = fixture.PixelsNearEdge(edge, fixture.Renderer.Palette.Critical);
            var plain = fixture.PixelsNearEdge(edge, fixture.Renderer.Palette.EdgeActual);

            Assert.IsTrue(plain > critical,
                $"Expected an estimate that was close to leave the arrow plain: {plain} plain pixels, {critical} critical.");
        }

        [TestMethod]
        public void Render_OutlinesABadEstimateInTheCriticalColourWithThePlainBodyUnderIt()
        {
            using var fixture = new RenderFixture();
            fixture.Controller.EdgeWidthBasis = PlanEdgeWidthBasis.Both;
            fixture.Render();

            var edge = WorstEstimate(fixture);
            var critical = fixture.PixelsNearEdge(edge, fixture.Renderer.Palette.Critical);
            var plain = fixture.PixelsNearEdge(edge, fixture.Renderer.Palette.EdgeActual);

            // The body goes back to plain where the outline carries the accuracy, or the dashes would
            // be drawn in the colour of the arrow they are meant to stand out against.
            Assert.IsTrue(plain > critical,
                $"Expected the body under the outline to be plain: {plain} plain pixels, {critical} critical.");

            Assert.IsTrue(critical > 20, $"Expected a critical coloured outline on the arrow, found {critical} pixels.");
        }

        [TestMethod]
        public void Render_DrawsTheTooltipOfAHoveredArrow()
        {
            using var fixture = new RenderFixture();
            var edge = fixture.Layout.Edges.First(e => e.From.Operator?.NodeId == 3);
            var middle = new LayoutPoint((edge.Points[0].X + edge.Points[1].X) / 2, edge.Points[0].Y);

            Assert.IsTrue(fixture.Controller.SetHover(fixture.Controller.ToScreen(middle)));
            fixture.Render();

            Assert.IsTrue(fixture.PixelsNear(fixture.Renderer.Palette.TooltipBackground, tolerance: 2) > 200,
                "No tooltip was drawn beside the arrow.");
        }

        [TestMethod]
        public void Render_SurvivesEveryZoomFromFittedToFarIn()
        {
            using var fixture = new RenderFixture();

            // The renderer drops detail below certain zooms and culls what is off screen; each of
            // those is a branch that only runs at some zooms, and a plan is read at all of them.
            foreach (var zoom in new[] { 0.1, 0.2, 0.3, 0.5, 0.6, 0.75, 1.0, 2.0, 8.0 })
            {
                fixture.Controller.SetZoom(zoom, new LayoutPoint(400, 300));
                fixture.Render();
            }
        }

        [TestMethod]
        public void Render_DrawsNothingButBackgroundWhenThePlanIsPannedOffScreen()
        {
            using var fixture = new RenderFixture();
            fixture.Controller.PanBy(-100000, -100000);
            fixture.Render();

            // Culling is what keeps a big plan cheap to redraw, and this is the case that proves the
            // cull is actually happening rather than just being harmless.
            Assert.AreEqual(0, fixture.DrawnPixelCount());
        }

        [TestMethod]
        public void Render_DrawsTheTooltipForAHoveredNode()
        {
            using var fixture = new RenderFixture();
            fixture.Render();
            var before = fixture.DrawnPixelCount();

            var node = fixture.Layout.Nodes.First(n => n.Operator?.NodeId == 1);
            fixture.Controller.SetHover(fixture.Controller.ToScreen(node.Bounds.Centre));
            fixture.Render();

            Assert.IsTrue(fixture.DrawnPixelCount() > before, "The tooltip added nothing to the picture.");
        }

        [TestMethod]
        public void Render_HandlesEverySelectionAndHighlightState()
        {
            using var fixture = new RenderFixture();

            foreach (var node in fixture.Layout.Nodes)
            {
                fixture.Controller.Select(node);
                fixture.Render();
            }

            fixture.Renderer.FadeOffPath = true;
            fixture.Controller.Select(fixture.Layout.Nodes[^1]);
            fixture.Render();

            fixture.Controller.Find("Scan");
            fixture.Render();
        }

        [TestMethod]
        public void Render_HandlesEveryMetricThePlanSupports()
        {
            using var fixture = new RenderFixture();

            foreach (var metric in Enum.GetValues<PlanHeatMetric>())
            {
                if (!fixture.Layout.Metrics.Supports(metric)) continue;

                fixture.Controller.HeatMetric = metric;
                fixture.Render();
            }
        }

        [TestMethod]
        public void Render_HandlesACollapsedPlan()
        {
            using var fixture = new RenderFixture();

            var withInputs = fixture.Layout.Nodes.First(n => n.Operator?.Children.Count > 0);
            fixture.Controller.ToggleCollapse(withInputs);
            fixture.Render();

            Assert.IsTrue(fixture.DrawnPixelCount() > 100);
        }

        /// <summary>Pixels that are not background in the strip just right of a node, where the stack behind a collapsed one peeks out.</summary>
        private static int PixelsBesideNode(RenderFixture fixture, string nodeId)
        {
            var node = fixture.Controller.Layout.Nodes.Single(n => n.Id == nodeId);
            var strip = fixture.Controller.ToScreen(new LayoutRect(node.Bounds.Right + 1, node.Bounds.Top + 6, 12, 12));

            using var bitmap = fixture.Snapshot();
            var background = fixture.Renderer.Palette.Background;
            var count = 0;

            for (var x = (int)strip.Left; x < (int)Math.Ceiling(strip.Right); x++)
            {
                for (var y = (int)strip.Top; y < (int)Math.Ceiling(strip.Bottom); y++)
                {
                    if (bitmap.GetPixel(x, y) != background) count++;
                }
            }

            return count;
        }

        [TestMethod]
        public void Render_ShowsACollapsedNodeAsAStackAtEveryZoom()
        {
            foreach (var zoom in new[] { 1.0, 0.2 })
            {
                using var fixture = new RenderFixture();
                fixture.Controller.SetZoom(zoom, new LayoutPoint(0, 0));

                var withInputs = fixture.Layout.Nodes.First(n => n.Operator?.Children.Count > 0);
                var id = withInputs.Id;

                fixture.Render();
                var expanded = PixelsBesideNode(fixture, id);

                fixture.Controller.ToggleCollapse(withInputs);
                fixture.Render();
                var collapsed = PixelsBesideNode(fixture, id);

                Assert.IsTrue(collapsed > expanded,
                    $"At zoom {zoom} the cards behind a collapsed node should show: {collapsed} pixels beside it collapsed, {expanded} expanded.");
            }
        }

        [TestMethod]
        public void Render_KeepsWarningsVisibleWhenZoomedFarOut()
        {
            using var fixture = new RenderFixture();

            // Far enough out that node content is not drawn at all - the glyph the marker sits on
            // is gone - which is exactly when the marker is how the problem gets found.
            fixture.Controller.SetZoom(0.2, new LayoutPoint(0, 0));
            fixture.Render();

            var sort = fixture.Layout.Nodes.Single(n => n.Operator?.NodeId == 1);
            var marker = fixture.Controller.ToScreen(sort.WarningMarkerBounds!.Value);
            var centre = marker.Centre;

            using var bitmap = fixture.Snapshot();
            Assert.IsTrue(centre.X >= 8 && centre.Y >= 8 && centre.X < bitmap.Width - 8 && centre.Y < bitmap.Height - 8,
                "The spilled sort should be on screen for this test to mean anything.");

            // The sort spilled, which is critical, so its triangle is red - and drawn at a size that
            // can be seen, though three pixels is all the layout's own size comes to at this zoom.
            var critical = fixture.Renderer.Palette.Critical;
            var red = 0;
            for (var x = (int)centre.X - 8; x <= (int)centre.X + 8; x++)
            {
                for (var y = (int)centre.Y - 8; y <= (int)centre.Y + 8; y++)
                {
                    if (bitmap.GetPixel(x, y) == critical) red++;
                }
            }

            Assert.IsTrue(red >= 20, $"Only {red} pixels of the warning colour were drawn.");
        }

        [TestMethod]
        public void Render_HandlesEverySamplePlan()
        {
            foreach (var plan in new[] { RenderFixture.KeyLookupSeek, RenderFixture.ParallelSpill, RenderFixture.Batch })
            {
                using var fixture = new RenderFixture(plan);
                fixture.Render();

                Assert.IsTrue(fixture.DrawnPixelCount() > 100, plan + " drew nothing.");
            }
        }

        [TestMethod]
        public void Render_HandlesBothPalettes()
        {
            using var fixture = new RenderFixture();

            foreach (var palette in new[] { PlanPalette.Light(), PlanPalette.Dark() })
            {
                fixture.Renderer.Palette = palette;
                fixture.Render();

                using var bitmap = fixture.Snapshot();
                Assert.AreEqual(palette.Background, bitmap.GetPixel(1, 1));
            }
        }

        [TestMethod]
        public void Render_RejectsNulls()
        {
            using var fixture = new RenderFixture();

            Assert.ThrowsExactly<ArgumentNullException>(() => fixture.Renderer.Render(null!, fixture.Controller));
            Assert.ThrowsExactly<ArgumentNullException>(() => fixture.Renderer.Render(fixture.Canvas, null!));
        }

        [TestMethod]
        public void MetricBarColour_RunsFromLowToHighAcrossTheRange()
        {
            var palette = PlanPalette.Light();

            Assert.AreEqual(palette.MetricBarLow, palette.MetricBarColour(0));
            Assert.AreEqual(palette.MetricBarMedium, palette.MetricBarColour(0.5));
            Assert.AreEqual(palette.MetricBarHigh, palette.MetricBarColour(1));

            // Out of range values are clamped rather than producing a colour from nowhere.
            Assert.AreEqual(palette.MetricBarLow, palette.MetricBarColour(-5));
            Assert.AreEqual(palette.MetricBarHigh, palette.MetricBarColour(5));
        }

        // ---------------------------------------------------------------- text elision

        [TestMethod]
        public void Elide_LeavesTextThatFitsAlone()
        {
            using var fonts = new PlanFonts();
            const string text = "Index Seek";

            var width = fonts.Title.MeasureText(text, (SKPaint?)null);
            Assert.AreEqual(text, PlanRenderer.Elide(text, fonts.Title, width + 1));
        }

        [TestMethod]
        public void Elide_CutsFromTheEndAndMarksIt()
        {
            using var fonts = new PlanFonts();
            const string text = "Clustered Index Seek on a table with a very long name indeed";

            var elided = PlanRenderer.Elide(text, fonts.Title, 60);

            Assert.AreNotEqual(text, elided);
            StringAssert.EndsWith(elided, "...");

            // Cut from the end, because the beginnings are what distinguish operator and object
            // names from one another.
            StringAssert.StartsWith(text, elided[..^3]);
            Assert.IsTrue(fonts.Title.MeasureText(elided, (SKPaint?)null) <= 60);
        }

        [TestMethod]
        public void Elide_DrawsNothingRatherThanAnEllipsisAlone()
        {
            using var fonts = new PlanFonts();

            // A node squeezed to nothing would otherwise show a row of dots, which says less than
            // an empty box does.
            Assert.AreEqual(string.Empty, PlanRenderer.Elide("Anything", fonts.Title, 2));
            Assert.AreEqual(string.Empty, PlanRenderer.Elide("Anything", fonts.Title, 0));
            Assert.AreEqual(string.Empty, PlanRenderer.Elide(string.Empty, fonts.Title, 100));
        }

        [TestMethod]
        public void Stacked_RendersWithEveryLineInsideItsNode()
        {
            var options = new PlanLayoutOptions();
            options.SetNodeWidth(PlanNodeWidth.Stacked);
            using var fixture = new RenderFixture(options: options);

            fixture.Render();

            // Each line the layout broke the text into fits the width it was given, measured with the
            // fonts it is drawn in - or it hit the widest a stacked node can be and is elided.
            foreach (var node in fixture.Layout.Nodes)
            {
                foreach (var line in node.TitleLines)
                {
                    var measured = fixture.Fonts.Title.MeasureText(line, (SKPaint?)null);
                    Assert.IsTrue(
                        measured <= node.TitleBounds.Width + 1 || node.Bounds.Width >= options.MaxNodeWidth - 1,
                        $"'{line}' measures {measured:0.#} in a box of {node.TitleBounds.Width:0.#}.");
                }
            }
        }

        // ---------------------------------------------------------------- tooltip value wrapping

        [TestMethod]
        public void WrapValue_KeepsEveryCharacterWhenItFitsTheLineLimit()
        {
            using var fonts = new PlanFonts();
            const string text = "[AdventureWorks].[Sales].[SalesOrderDetail].[ProductID] = Scalar Operator([@ProductID]), [AdventureWorks].[Sales].[SalesOrderDetail].[SalesOrderID] >= Scalar Operator((43659))";

            var lines = PlanRenderer.WrapValue(text, 200, fonts.TooltipText, 20);

            Assert.IsTrue(lines.Count > 1, "A value wider than the line should wrap.");
            Assert.IsTrue(lines.All(l => fonts.TooltipText.MeasureText(l, (SKPaint?)null) <= 200), "No line may be wider than asked.");

            // Long names with no spaces are broken rather than elided, so nothing is lost.
            Assert.AreEqual(text.Replace(" ", string.Empty), string.Concat(lines).Replace(" ", string.Empty));
        }

        [TestMethod]
        public void WrapValue_StopsAtTheLineLimitWithAnEllipsis()
        {
            using var fonts = new PlanFonts();
            var text = string.Join(" AND ", Enumerable.Range(1, 60).Select(i => "[T].[Column" + i + "] = (" + i + ")"));

            var lines = PlanRenderer.WrapValue(text, 200, fonts.TooltipText, 4);

            Assert.AreEqual(4, lines.Count);
            StringAssert.EndsWith(lines[^1], "...");
        }

        [TestMethod]
        public void WrapValue_LeavesAShortValueOnOneLine()
        {
            using var fonts = new PlanFonts();

            CollectionAssert.AreEqual(new[] { "[T].[a] = (1)" }, PlanRenderer.WrapValue("[T].[a] = (1)", 400, fonts.TooltipText, 8));
        }

        // ---------------------------------------------------------------- measurement

        [TestMethod]
        public void Measurer_GivesEachRoleItsOwnFont()
        {
            using var fonts = new PlanFonts();
            var measurer = new SkiaPlanTextMeasurer(fonts);

            const string text = "Clustered Index Scan";

            var title = measurer.Measure(text, PlanTextRole.Title);
            var detail = measurer.Measure(text, PlanTextRole.Detail);
            var metric = measurer.Measure(text, PlanTextRole.Metric);

            // The title is bold and larger, so it must measure wider than the lines beneath it - if
            // they all measured the same, the node would be sized for the wrong text.
            Assert.IsTrue(title.Width > detail.Width);
            Assert.IsTrue(detail.Width > metric.Width);
            Assert.IsTrue(title.Height > metric.Height);
        }

        [TestMethod]
        public void Measurer_HandlesEmptyText()
        {
            using var fonts = new PlanFonts();
            var measurer = new SkiaPlanTextMeasurer(fonts);

            var size = measurer.Measure(string.Empty, PlanTextRole.Title);

            Assert.AreEqual(0, size.Width);

            // Height is the line height regardless, so an empty line still occupies its row rather
            // than collapsing the node around it.
            Assert.IsTrue(size.Height > 0);
        }

        [TestMethod]
        public void Measurer_RejectsANullFontSet()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new SkiaPlanTextMeasurer(null!));
        }

        [TestMethod]
        public void LayoutAndRendererMustShareTheirFonts()
        {
            // The layout sizes nodes from measured text, so text drawn with a different font lands
            // outside the box measured for it.  This is the invariant the shared PlanFonts exists
            // to hold; the check is that the title actually fits the space the layout gave it.
            using var fixture = new RenderFixture();

            foreach (var node in fixture.Layout.Nodes)
            {
                var measured = fixture.Fonts.Title.MeasureText(node.Title, (SKPaint?)null);
                var available = node.TitleBounds.Width;

                // Either it fits, or it was elided because the node hit its maximum width - what
                // must never happen is text measured with one font and drawn with another, which
                // would show up here as a title far wider than its box on an unconstrained node.
                Assert.IsTrue(
                    measured <= available + 1 || node.Bounds.Width >= 299,
                    $"'{node.Title}' measures {measured:0.#} in a box of {available:0.#}.");
            }
        }
    }
}
