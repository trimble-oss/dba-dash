using System;
using System.Linq;
using DBADash.QueryPlan.Live;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Skia.Test
{
    [TestClass]
    public class PlanRendererLiveTests
    {
        /// <summary>A capture of every operator in the fixture's plan, with or without rows moving since the one before.</summary>
        private static PlanLiveSnapshot Snapshot(RenderFixture fixture, bool moving, bool started = true)
        {
            var ids = fixture.Statement.RootOperator!.DescendantsAndSelf().Select(o => o.NodeId).ToList();
            var now = ids.Select(id => new PlanLiveCounters { NodeId = id, Rows = started ? 100 : 0, Rebinds = started ? 1 : 0 }).ToList();
            var before = ids.Select(id => new PlanLiveCounters { NodeId = id, Rows = moving ? 50 : 100, Rebinds = 1 }).ToList();

            return PlanLiveSnapshot.From(now, before, DateTime.UtcNow);
        }

        [TestMethod]
        public void Render_HighlightsOperatorsMovingRows()
        {
            using var fixture = new RenderFixture();
            fixture.Render();
            var plain = fixture.PixelsNear(fixture.Renderer.Palette.Info, tolerance: 8);

            fixture.Controller.SetLive(Snapshot(fixture, moving: true));
            fixture.Render();
            var live = fixture.PixelsNear(fixture.Renderer.Palette.Info, tolerance: 8);

            Assert.IsTrue(live > plain + 500, $"Expected active borders in the info colour: {live} pixels live, {plain} without.");
        }

        [TestMethod]
        public void Render_DimsOperatorsThatHaveNotStarted()
        {
            using var fixture = new RenderFixture();
            fixture.Render();
            var plain = fixture.PixelsNear(fixture.Renderer.Palette.TitleText, tolerance: 8);

            fixture.Controller.SetLive(Snapshot(fixture, moving: false, started: false));
            fixture.Render();
            var dimmed = fixture.PixelsNear(fixture.Renderer.Palette.TitleText, tolerance: 8);

            Assert.IsTrue(dimmed < plain / 2, $"Expected titles faded towards the background: {dimmed} full colour pixels, {plain} without.");
        }

        [TestMethod]
        public void Render_FlowMovesWithThePhase()
        {
            using var fixture = new RenderFixture();
            fixture.Controller.SetLive(Snapshot(fixture, moving: true));

            fixture.Renderer.FlowPhase = 0;
            fixture.Render();
            using var first = fixture.Snapshot();

            fixture.Renderer.FlowPhase = 0.2;
            fixture.Render();
            using var second = fixture.Snapshot();

            var different = 0;
            for (var x = 0; x < first.Width; x += 2)
            {
                for (var y = 0; y < first.Height; y += 2)
                {
                    if (first.GetPixel(x, y) != second.GetPixel(x, y)) different++;
                }
            }

            Assert.IsTrue(different > 50, $"Expected the dashes along active arrows to move: {different} pixels changed.");
        }

        [TestMethod]
        public void Render_WithoutLiveStateDrawsAsBefore()
        {
            using var fixture = new RenderFixture();
            fixture.Render();
            using var before = fixture.Snapshot();

            fixture.Renderer.FlowPhase = 0.7;
            fixture.Controller.SetLive(null);
            fixture.Render();
            using var after = fixture.Snapshot();

            Assert.IsTrue(before.Bytes.SequenceEqual(after.Bytes), "The live drawing should only apply to a live plan.");
        }
    }
}
