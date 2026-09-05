using System;
using System.Linq;
using DBADash.Deadlock.Interaction;
using DBADash.Deadlock.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace DBADash.Deadlock.Skia.Test
{
    /// <summary>
    /// Renders to a bitmap and asserts on the pixels, which exercises the whole pipeline end to end:
    /// parse, layout with real font metrics, the view transform, and the drawing itself.  A distinct
    /// opaque colour per palette entry makes "was this actually drawn, and where" answerable.
    /// </summary>
    [TestClass]
    public class DeadlockRendererTests
    {
        private static RenderFixture KeyLock(DeadlockRenderStyle? style = null) =>
            new(RenderFixture.LoadGraph("KeyLockDeadlock"), style);

        private static RenderFixture SharedLock(DeadlockRenderStyle? style = null) =>
            new(RenderFixture.LoadGraph("SharedLockDeadlock"), style);

        private static DeadlockProcessNode Victim(RenderFixture fixture) =>
            fixture.Layout.Nodes.OfType<DeadlockProcessNode>().Single(n => n.IsVictim);

        private static DeadlockProcessNode Process(RenderFixture fixture, int spid) =>
            fixture.Layout.Nodes.OfType<DeadlockProcessNode>().Single(n => n.Process.Spid == spid);

        /// <summary>
        /// Counts a colour with nothing selected, and again with <paramref name="spid"/> selected.
        /// Everything about the two renders is the same bar the selection, so the difference between
        /// the counts is what selecting did - see the note on <see cref="TestPalette"/> for why the
        /// assertions here are comparative.
        /// </summary>
        private static (int Unselected, int Selected) CountBySelection(
            RenderFixture fixture, SKColor colour, int spid)
        {
            fixture.Controller.SetViewport(new LayoutSize(900, 700));
            fixture.Controller.ZoomToFit();

            int unselected;
            using (var before = fixture.Render())
            {
                unselected = BitmapAssertions.Count(before, colour);
            }

            fixture.Controller.Select(Process(fixture, spid));
            using var after = fixture.Render();

            return (unselected, BitmapAssertions.Count(after, colour));
        }

        // ---------------------------------------------------------------- basics

        [TestMethod]
        public void Render_FillsTheBackground()
        {
            using var fixture = KeyLock();
            using var bitmap = fixture.RenderFitted();

            Assert.AreEqual(TestPalette.Background, bitmap.GetPixel(0, 0));
        }

        [TestMethod]
        public void Render_EmptyLayout_OnlyClearsTheBackground()
        {
            using var fixture = new RenderFixture("<deadlock><process-list /></deadlock>");
            using var bitmap = fixture.Render(200, 150);

            Assert.IsFalse(
                BitmapAssertions.Contains(bitmap, TestPalette.ProcessFill),
                "Nothing should be drawn for a deadlock with no processes or resources.");
            Assert.AreEqual(TestPalette.Background, bitmap.GetPixel(100, 75));
        }

        [TestMethod]
        public void Render_DrawsProcessResourceAndVictimNodes()
        {
            using var fixture = KeyLock();
            using var bitmap = fixture.RenderFitted();

            Assert.IsTrue(BitmapAssertions.Contains(bitmap, TestPalette.VictimFill), "Victim fill missing.");
            Assert.IsTrue(BitmapAssertions.Contains(bitmap, TestPalette.ProcessFill), "Process fill missing.");
            Assert.IsTrue(BitmapAssertions.Contains(bitmap, TestPalette.ResourceFill), "Resource fill missing.");
        }

        [TestMethod]
        public void Render_PutsTheVictimColourInsideTheVictimNode()
        {
            // Verifies the transform pipeline, not just that something was painted: the colour has to
            // land where the view says the node is.
            using var fixture = KeyLock();
            using var bitmap = fixture.RenderFitted();

            var victimOnScreen = fixture.Controller.ToScreen(Victim(fixture).Bounds);

            Assert.IsTrue(BitmapAssertions.ContainsWithin(bitmap, victimOnScreen, TestPalette.VictimFill));
        }

        [TestMethod]
        public void Render_DrawsTheCycleEdgesInTheCycleColour()
        {
            using var fixture = KeyLock();
            using var bitmap = fixture.RenderFitted();

            Assert.IsTrue(fixture.Layout.Edges.All(e => e.IsInCycle), "This sample is a clean cycle.");
            Assert.IsTrue(BitmapAssertions.Contains(bitmap, TestPalette.CycleEdge));
        }

        [TestMethod]
        public void Render_DrawsIncidentalEdgesInTheOrdinaryColour()
        {
            // The truncated sample has no closed cycle, so every edge is incidental.
            using var fixture = new RenderFixture(RenderFixture.LoadGraph("TruncatedGraph"));
            using var bitmap = fixture.RenderFitted();

            // That no edge is on the cycle is asserted exactly against the model; the pixels only
            // need to confirm the ordinary edge colour actually reached the canvas.
            Assert.IsFalse(fixture.Layout.Edges.Any(e => e.IsInCycle));
            Assert.IsTrue(BitmapAssertions.Contains(bitmap, TestPalette.Edge));
        }

        // ---------------------------------------------------------------- view transform

        [TestMethod]
        public void Render_FollowsPanAndZoom()
        {
            using var fixture = KeyLock();
            fixture.Controller.SetViewport(new LayoutSize(900, 700));
            fixture.Controller.SetZoom(0.8, new LayoutPoint(0, 0));
            fixture.Controller.PanBy(40, 25);

            using var bitmap = fixture.Render();
            var victimOnScreen = fixture.Controller.ToScreen(Victim(fixture).Bounds);

            Assert.IsTrue(
                BitmapAssertions.ContainsWithin(bitmap, victimOnScreen, TestPalette.VictimFill),
                "The node should be drawn where the current pan and zoom put it.");
        }

        [TestMethod]
        public void Render_ContentScrolledOffScreenIsNotDrawn()
        {
            using var fixture = KeyLock();
            fixture.Controller.SetViewport(new LayoutSize(900, 700));
            fixture.Controller.PanBy(-50000, -50000);

            using var bitmap = fixture.Render();

            Assert.IsFalse(BitmapAssertions.Contains(bitmap, TestPalette.VictimFill));
            Assert.AreEqual(TestPalette.Background, bitmap.GetPixel(450, 350));
        }

        // ---------------------------------------------------------------- zoom thresholds

        /// <summary>
        /// Renders the same graph at the same zoom twice, differing only in the threshold, and
        /// returns how many pixels of <paramref name="colour"/> each produced.
        ///
        /// Comparative rather than absolute because antialiasing makes an absolute "this colour is
        /// absent" assertion unsound: the blend along the boundary between two palette entries
        /// passes through intermediate colours that can coincide with a third.  Holding everything
        /// but the threshold constant means that baseline is identical in both renders, so a
        /// difference can only come from the behaviour under test.
        /// </summary>
        private static (int Drawn, int Omitted) CountByThreshold(
            double zoom, SKColor colour, Func<double, DeadlockRenderStyle> styleFor)
        {
            int Count(double threshold)
            {
                using var fixture = KeyLock(styleFor(threshold));
                fixture.Controller.SetViewport(new LayoutSize(900, 700));
                fixture.Controller.SetZoom(zoom, new LayoutPoint(0, 0));
                using var bitmap = fixture.Render();
                return BitmapAssertions.Count(bitmap, colour);
            }

            return (Count(zoom / 2), Count(zoom * 2));
        }

        [TestMethod]
        public void Render_OmitsDetailLinesBelowTheZoomThreshold()
        {
            var (drawn, omitted) = CountByThreshold(
                0.8,
                TestPalette.DetailText,
                threshold => new DeadlockRenderStyle { MinZoomForDetailLines = threshold });

            Assert.IsTrue(drawn > omitted,
                $"Detail lines should add pixels above the threshold and none below (drawn {drawn}, omitted {omitted}).");
        }

        [TestMethod]
        public void Render_OmitsEdgeLabelsBelowTheZoomThreshold()
        {
            var (drawn, omitted) = CountByThreshold(
                0.8,
                TestPalette.EdgeLabelBackground,
                threshold => new DeadlockRenderStyle { MinZoomForEdgeLabels = threshold });

            Assert.IsTrue(drawn > omitted,
                $"Edge labels should add pixels above the threshold and none below (drawn {drawn}, omitted {omitted}).");
        }

        // ---------------------------------------------------------------- selection and hover

        [TestMethod]
        public void Render_SelectedNodeIsOutlinedInTheSelectionColour()
        {
            using var fixture = KeyLock();
            fixture.Controller.SetViewport(new LayoutSize(900, 700));
            fixture.Controller.ZoomToFit();

            int unselected;
            using (var before = fixture.Render())
            {
                unselected = BitmapAssertions.Count(before, TestPalette.Selection);
            }

            fixture.Controller.Select(Victim(fixture));
            using var after = fixture.Render();

            // Comparative, for the reason given on TestPalette: only the selection differs between
            // the two renders, so any increase is the selection outline.
            Assert.IsTrue(
                BitmapAssertions.Count(after, TestPalette.Selection) > unselected,
                "Selecting a node should add selection-coloured pixels.");
        }

        [TestMethod]
        public void Render_HoveredNodeIsOutlinedAndGetsATooltip()
        {
            using var fixture = KeyLock();
            fixture.Controller.SetViewport(new LayoutSize(900, 700));
            fixture.Controller.ZoomToFit();

            var victim = Victim(fixture);
            fixture.Controller.SetHover(fixture.Controller.ToScreen(victim.Bounds.Centre));

            using var bitmap = fixture.Render();

            Assert.IsTrue(BitmapAssertions.Contains(bitmap, TestPalette.Hover), "Hover outline missing.");
            Assert.IsTrue(
                BitmapAssertions.Contains(bitmap, TestPalette.TooltipBackground), "Tooltip missing.");
        }

        [TestMethod]
        public void Render_NoTooltipWhenNothingIsHovered()
        {
            using var fixture = KeyLock();
            using var bitmap = fixture.RenderFitted();

            Assert.IsFalse(BitmapAssertions.Contains(bitmap, TestPalette.TooltipBackground));
        }

        [TestMethod]
        public void Render_TooltipStaysInsideTheViewport()
        {
            // A node against the right edge must flip its tooltip to the other side rather than let
            // it run off the surface.
            using var fixture = KeyLock();
            var viewport = new LayoutSize(900, 700);
            fixture.Controller.SetViewport(viewport);
            fixture.Controller.ZoomToFit();

            var rightmost = fixture.Layout.Nodes.OrderByDescending(n => n.Bounds.Right).First();
            fixture.Controller.SetHover(fixture.Controller.ToScreen(rightmost.Bounds.Centre));

            using var bitmap = fixture.Render(900, 700);

            // A tooltip placed off the right edge would be clipped away entirely, so finding a
            // substantial amount of it is the check that it flipped to the node's other side.
            Assert.IsTrue(
                BitmapAssertions.Count(bitmap, TestPalette.TooltipBackground) > 500,
                "The tooltip should have been flipped inside the viewport rather than clipped.");
        }

        [TestMethod]
        public void Render_TooltipIsNotScaledByZoom()
        {
            // The tooltip is drawn in screen space so it stays readable however far the graph is
            // zoomed out; its height must therefore not change with zoom.
            //
            // Measured on a surface with room to spare: the tooltip is placed relative to the node,
            // which does move with zoom, so on a tight surface it can end up partly off screen and
            // the visible extent would then be measuring the clipping rather than the tooltip.
            const int width = 1600;
            const int height = 1100;

            using var fixture = KeyLock();
            fixture.Controller.SetViewport(new LayoutSize(width, height));

            int HeightAtZoom(double zoom)
            {
                fixture.Controller.SetZoom(zoom, new LayoutPoint(0, 0));
                fixture.Controller.ClearHover();
                var victim = Victim(fixture);
                fixture.Controller.SetHover(fixture.Controller.ToScreen(victim.Bounds.Centre));

                using var bitmap = fixture.Render(width, height);
                var extent = BitmapAssertions.VerticalExtent(bitmap, TestPalette.TooltipBackground);
                Assert.IsNotNull(extent, $"No tooltip was drawn at zoom {zoom}.");

                Assert.IsTrue(
                    extent!.Value.Top > 0 && extent.Value.Bottom < height - 1,
                    $"The tooltip was clipped at zoom {zoom}, so its height cannot be compared.");

                return extent.Value.Bottom - extent.Value.Top;
            }

            var atFullSize = HeightAtZoom(1.0);
            var atHalfSize = HeightAtZoom(0.5);

            Assert.IsTrue(atFullSize > 0);
            Assert.AreEqual(atFullSize, atHalfSize);
        }

        [TestMethod]
        public void Render_TooltipDoesNotCoverTheNodeItDescribes()
        {
            // The tooltip is wider than the space beside a node near the middle of the graph, so it
            // has to fall back to below rather than being clamped over the node itself.
            using var fixture = KeyLock();
            fixture.Controller.SetViewport(new LayoutSize(900, 900));
            fixture.Controller.ZoomToFit();

            var victim = Victim(fixture);
            fixture.Controller.SetHover(fixture.Controller.ToScreen(victim.Bounds.Centre));

            using var bitmap = fixture.Render(900, 900);
            var onScreen = fixture.Controller.ToScreen(victim.Bounds);

            Assert.IsTrue(
                BitmapAssertions.ContainsWithin(bitmap, onScreen, TestPalette.VictimFill),
                "The hovered node should still be visible, not hidden behind its own tooltip.");
        }

        [TestMethod]
        public void Render_TooltipGrowsWithItsFontSize()
        {
            int TooltipHeight(float fontSize)
            {
                using var fixture = KeyLock(new DeadlockRenderStyle
                {
                    TooltipFontSize = fontSize,
                    TooltipTitleFontSize = fontSize + 2
                });
                fixture.Controller.SetViewport(new LayoutSize(900, 700));
                fixture.Controller.ZoomToFit();
                fixture.Controller.SetHover(fixture.Controller.ToScreen(Victim(fixture).Bounds.Centre));

                using var bitmap = fixture.Render();
                var extent = BitmapAssertions.VerticalExtent(bitmap, TestPalette.TooltipBackground);
                Assert.IsNotNull(extent);
                return extent!.Value.Bottom - extent.Value.Top;
            }

            Assert.IsTrue(
                TooltipHeight(16f) > TooltipHeight(9f),
                "Larger tooltip text should produce a taller tooltip.");
        }

        // ---------------------------------------------------------------- robustness

        [TestMethod]
        [DataRow("KeyLockDeadlock")]
        [DataRow("DeadlockListWrapper")]
        [DataRow("ParallelDeadlock")]
        [DataRow("ThreeWayDeadlock")]
        [DataRow("TruncatedGraph")]
        [DataRow("XeEventEnvelope")]
        public void Render_EverySampleDrawsSomething(string sampleName)
        {
            using var fixture = new RenderFixture(RenderFixture.LoadGraph(sampleName));
            using var bitmap = fixture.RenderFitted();

            var painted = false;
            for (var x = 0; x < bitmap.Width && !painted; x += 3)
            {
                for (var y = 0; y < bitmap.Height && !painted; y += 3)
                {
                    if (bitmap.GetPixel(x, y) != TestPalette.Background) painted = true;
                }
            }

            Assert.IsTrue(painted, $"{sampleName} rendered as a blank surface.");
        }

        [TestMethod]
        [DataRow(0.1)]
        [DataRow(0.5)]
        [DataRow(1.0)]
        [DataRow(4.0)]
        [DataRow(8.0)]
        public void Render_SurvivesExtremeZoomLevels(double zoom)
        {
            using var fixture = KeyLock();
            fixture.Controller.SetViewport(new LayoutSize(400, 300));
            fixture.Controller.SetZoom(zoom, new LayoutPoint(200, 150));
            fixture.Controller.SetHover(new LayoutPoint(200, 150));

            using var bitmap = fixture.Render(400, 300);

            Assert.AreEqual(400, bitmap.Width);
        }

        // ---------------------------------------------------------------- highlighting the selection

        /// <summary>
        /// SPID 55 holds the Invoice key, so selecting it should colour that arrow and that box as
        /// ownership - the relationship a deadlock is usually being read to find.
        /// </summary>
        [TestMethod]
        public void Render_SelectedProcess_ColoursWhatItHolds()
        {
            using var fixture = SharedLock();

            var (unselected, selected) = CountBySelection(fixture, TestPalette.OwnerHighlight, 55);

            Assert.IsTrue(selected > unselected,
                $"Selecting a process should colour what it holds (before {unselected}, after {selected}).");
        }

        /// <summary>And the Customer key it is blocked on, in the other colour.</summary>
        [TestMethod]
        public void Render_SelectedProcess_ColoursWhatItIsWaitingFor()
        {
            using var fixture = SharedLock();

            var (unselected, selected) = CountBySelection(fixture, TestPalette.WaiterHighlight, 55);

            Assert.IsTrue(selected > unselected,
                $"Selecting a process should colour what it waits for (before {unselected}, after {selected}).");
        }

        /// <summary>
        /// The point of the whole thing: on a big graph the boxes that have nothing to do with the
        /// selection have to step back, or the highlighted ones are still lost in the crowd.
        /// </summary>
        [TestMethod]
        public void Render_Selection_FadesUnrelatedNodesAndEdges()
        {
            using var fixture = SharedLock();
            var (unselectedFill, selectedFill) = CountBySelection(fixture, TestPalette.ProcessFill, 55);

            using var edges = SharedLock();
            var (unselectedEdge, selectedEdge) = CountBySelection(edges, TestPalette.Edge, 55);

            Assert.IsTrue(selectedFill < unselectedFill,
                $"Unrelated node fill should fade (before {unselectedFill}, after {selectedFill}).");
            Assert.IsTrue(selectedEdge < unselectedEdge,
                $"Unrelated edges should fade (before {unselectedEdge}, after {selectedEdge}).");
        }

        /// <summary>
        /// Fading is a rendering decision, not a layout one, so turning it off has to leave the
        /// picture exactly as it was.  That is also what keeps the fade out of the way of the tests
        /// above it, which know nothing about selection.
        /// </summary>
        [TestMethod]
        public void Render_WithFadeDisabled_LeavesUnrelatedNodesAlone()
        {
            using var faded = SharedLock();
            using var plain = SharedLock(new DeadlockRenderStyle { UnrelatedFade = 0f });

            var withFade = CountBySelection(faded, TestPalette.ProcessFill, 55).Selected;
            var withoutFade = CountBySelection(plain, TestPalette.ProcessFill, 55);

            Assert.IsTrue(withoutFade.Selected > withFade,
                $"Turning the fade off should leave unrelated fill drawn (faded {withFade}, plain {withoutFade.Selected}).");
            Assert.AreEqual(withoutFade.Unselected, withoutFade.Selected,
                "With the fade off, selecting a node should not change unrelated fill at all.");
        }

        [TestMethod]
        public void Render_NullArguments_Throw()
        {
            using var fixture = KeyLock();
            using var bitmap = new SKBitmap(10, 10);
            using var canvas = new SKCanvas(bitmap);

            Assert.ThrowsExactly<ArgumentNullException>(() => fixture.Renderer.Render(null!, fixture.Controller));
            Assert.ThrowsExactly<ArgumentNullException>(() => fixture.Renderer.Render(canvas, null!));
        }

        [TestMethod]
        public void Constructor_NullFonts_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DeadlockRenderer(null!));
        }
    }
}
