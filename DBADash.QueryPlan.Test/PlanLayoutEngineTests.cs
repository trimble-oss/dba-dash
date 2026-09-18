using System;
using System.Linq;
using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DBADash.QueryPlan.Test.InlinePlan;

namespace DBADash.QueryPlan.Test
{
    [TestClass]
    public class PlanLayoutEngineTests
    {
        private static PlanLayout Layout(string plan, PlanLayoutOptions? options = null) =>
            new PlanLayoutEngine(new FakeTextMeasurer(), options).Layout(TestPlans.Statement(plan));

        private static PlanLayout Layout(PlanStatement statement) =>
            new PlanLayoutEngine(new FakeTextMeasurer()).Layout(statement);

        [TestMethod]
        public void Layout_AddsASyntheticRootAheadOfThePlansOwnRoot()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);

            // SQL Server emits no operator for the statement itself, but a plan is a pipeline that
            // ends somewhere and the reader needs to see where.
            Assert.IsTrue(layout.Root.IsRoot);
            Assert.IsNull(layout.Root.Operator);
            Assert.AreEqual("SELECT", layout.Root.Title);
            Assert.AreEqual(PlanOperatorKind.StatementRoot, layout.Root.Kind);

            // Three operators plus the synthetic root.
            Assert.AreEqual(4, layout.Nodes.Count);
            Assert.AreSame(layout.Root, layout.Nodes[0]);
        }

        [TestMethod]
        public void Layout_PutsTheRootOnTheLeftAndInputsToTheRight()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);

            // Rows flow right to left, into the root, which is the convention every SQL Server DBA
            // already reads fluently.
            foreach (var node in layout.Nodes.Where(n => n.Parent is not null))
            {
                Assert.IsTrue(
                    node.Bounds.Left > node.Parent!.Bounds.Right,
                    $"{node.Title} should sit to the right of its consumer {node.Parent.Title}.");
            }
        }

        [TestMethod]
        public void Layout_GivesEveryNodeInAColumnTheSameWidth()
        {
            var layout = Layout(TestPlans.ParallelSpill);

            foreach (var column in layout.Nodes.GroupBy(n => n.Depth))
            {
                var widths = column.Select(n => n.Bounds.Width).Distinct().ToList();
                Assert.AreEqual(1, widths.Count, $"Column {column.Key} has ragged widths.");
            }
        }

        [TestMethod]
        public void Layout_CanSizeEachNodeToItsOwnContentInstead()
        {
            var layout = Layout(TestPlans.ParallelSpill, new PlanLayoutOptions { UniformColumnWidths = false });

            var widths = layout.Nodes.Select(n => n.Bounds.Width).Distinct().Count();
            Assert.IsTrue(widths > 1, "With uniform widths off, nodes should differ.");
        }

        [TestMethod]
        public void Layout_KeepsEveryNodeWithinTheChosenWidthPreset()
        {
            foreach (var width in Enum.GetValues<PlanNodeWidth>())
            {
                var options = new PlanLayoutOptions();
                options.SetNodeWidth(width);
                var (min, max) = PlanNodeWidths.Range(width);

                foreach (var node in Layout(TestPlans.ParallelSpill, options).Nodes)
                {
                    Assert.IsTrue(node.Bounds.Width >= min && node.Bounds.Width <= max,
                        $"{node.Title} is {node.Bounds.Width} wide at {width}, outside {min}-{max}.");
                }
            }
        }

        [TestMethod]
        public void NodeWidthPresets_GetWiderInOrderAndNormalIsTheDefault()
        {
            var defaults = new PlanLayoutOptions();
            Assert.AreEqual((defaults.MinNodeWidth, defaults.MaxNodeWidth), PlanNodeWidths.Range(PlanNodeWidth.Normal));

            var ranges = Enum.GetValues<PlanNodeWidth>().Select(PlanNodeWidths.Range).ToList();
            for (var i = 1; i < ranges.Count; i++)
            {
                Assert.IsTrue(ranges[i].Min >= ranges[i - 1].Min && ranges[i].Max > ranges[i - 1].Max);
            }

            Assert.IsTrue(ranges.All(r => r.Min < r.Max));
        }

        private static PlanLayout Stacked(string plan)
        {
            var options = new PlanLayoutOptions();
            options.SetNodeWidth(PlanNodeWidth.Stacked);
            return Layout(plan, options);
        }

        [TestMethod]
        public void Stacked_PutsTheIconCentredAboveTheText()
        {
            foreach (var node in Stacked(TestPlans.ParallelSpill).Nodes)
            {
                Assert.IsTrue(node.CentreText, node.Title);
                Assert.AreEqual(node.Bounds.Centre.X, node.IconBounds.Centre.X, 0.001, node.Title);
                Assert.IsTrue(node.TitleBounds.Top >= node.IconBounds.Bottom, node.Title);

                // Everything inside the node, above the bar.
                foreach (var text in new[] { node.TitleBounds, node.SubtitleBounds, node.MetricBounds, node.TimingBounds }.OfType<LayoutRect>())
                {
                    Assert.IsTrue(text.Left >= node.Bounds.Left && text.Right <= node.Bounds.Right, node.Title);
                    Assert.IsTrue(text.Bottom <= (node.MetricBarBounds?.Top ?? node.Bounds.Bottom) + 0.001, node.Title);
                }
            }
        }

        [TestMethod]
        public void Stacked_WrapsALongNameOntoTwoLinesRatherThanWideningTheNode()
        {
            var measurer = new FakeTextMeasurer();
            var (_, max) = PlanNodeWidths.Range(PlanNodeWidth.Stacked);
            var available = max - (new PlanLayoutOptions().NodePadding * 2);

            var wide = Stacked(TestPlans.ParallelSpill).Nodes
                .Where(n => n.Title.Contains(' ') && measurer.Measure(n.Title, PlanTextRole.Title).Width > available)
                .ToList();

            Assert.IsTrue(wide.Count > 0, "The sample plan should have a name too long for one line.");

            foreach (var node in wide)
            {
                Assert.AreEqual(2, node.TitleLines.Count, node.Title);
                Assert.AreEqual(node.Title, string.Join(" ", node.TitleLines));
                Assert.IsTrue(node.Bounds.Width <= max, node.Title);
                Assert.AreEqual(2 * measurer.Measure("x", PlanTextRole.Title).Height, node.TitleBounds.Height, 0.001, node.Title);
            }

            // At the break that leaves the longer line shortest.
            foreach (var node in wide)
            {
                double Longer(string a, string b) =>
                    Math.Max(measurer.Measure(a, PlanTextRole.Title).Width, measurer.Measure(b, PlanTextRole.Title).Width);

                var words = node.Title.Split(' ');
                var best = Enumerable.Range(1, words.Length - 1)
                    .Min(i => Longer(string.Join(" ", words[..i]), string.Join(" ", words[i..])));

                Assert.AreEqual(best, Longer(node.TitleLines[0], node.TitleLines[1]), 0.001, node.Title);
            }
        }

        [TestMethod]
        public void Stacked_SplitsFiguresThatDoNotFitAtTheirSeparators()
        {
            var layout = Stacked(TestPlans.ParallelSpill);

            foreach (var node in layout.Nodes.Where(n => n.TimingLine is not null))
            {
                Assert.AreEqual(node.TimingLine, string.Join(" · ", node.TimingLines), "Nothing lost in the split.");
            }

            Assert.IsTrue(layout.Nodes.Any(n => n.TimingLines.Count > 1), "The elapsed and CPU times should need a line each.");
        }

        [TestMethod]
        public void Stacked_IsNarrowerThanTheIconBesideTheText()
        {
            var stacked = Stacked(TestPlans.ParallelSpill);

            var options = new PlanLayoutOptions();
            options.SetNodeWidth(PlanNodeWidth.SuperCompact);
            var beside = Layout(TestPlans.ParallelSpill, options);

            Assert.IsTrue(stacked.Bounds.Width < beside.Bounds.Width, $"{stacked.Bounds.Width} vs {beside.Bounds.Width}");
        }

        private static PlanLayout WrappedNames(PlanNodeWidth width, int maxLines = 4)
        {
            var options = new PlanLayoutOptions { MaxObjectNameLines = maxLines };
            options.SetNodeWidth(width);
            options.WrapObjectNames = true;
            return Layout(TestPlans.ParallelSpill, options);
        }

        [TestMethod]
        public void WrapObjectNames_WrapsANameTooLongForTheNodeWithoutLosingAnyOfIt()
        {
            foreach (var width in new[] { PlanNodeWidth.Compact, PlanNodeWidth.Stacked })
            {
                var wrapped = WrappedNames(width).Nodes.Where(n => n.SubtitleLines.Count > 1).ToList();
                Assert.IsTrue(wrapped.Count > 0, $"Something should wrap at {width}.");

                foreach (var node in wrapped)
                {
                    // Only the break's own space is dropped.
                    Assert.AreEqual(node.Subtitle!.Replace(" ", string.Empty), string.Concat(node.SubtitleLines).Replace(" ", string.Empty));

                    var measurer = new FakeTextMeasurer();
                    foreach (var line in node.SubtitleLines)
                    {
                        Assert.IsTrue(measurer.Measure(line, PlanTextRole.Detail).Width <= node.SubtitleBounds!.Value.Width + 0.001, $"'{line}' at {width}");
                    }

                    Assert.AreEqual(node.SubtitleLines.Count * measurer.Measure("x", PlanTextRole.Detail).Height, node.SubtitleBounds!.Value.Height, 0.001);
                }
            }
        }

        [TestMethod]
        public void WrapObjectNames_BreaksAfterADotOrAtASpace()
        {
            foreach (var node in WrappedNames(PlanNodeWidth.Compact).Nodes.Where(n => n.SubtitleLines.Count > 1))
            {
                var name = node.Subtitle!;
                var position = 0;

                foreach (var line in node.SubtitleLines.Take(node.SubtitleLines.Count - 1))
                {
                    Assert.AreEqual(position, name.IndexOf(line, position, StringComparison.Ordinal), name);
                    position += line.Length;

                    Assert.IsTrue(line.EndsWith('.') || name[position] == ' ',
                        $"'{name}' split as {string.Join(" | ", node.SubtitleLines)}");

                    // The alias stays with its AS.
                    Assert.IsFalse(line.EndsWith(" AS", StringComparison.Ordinal), $"'{name}' split as {string.Join(" | ", node.SubtitleLines)}");

                    while (position < name.Length && name[position] == ' ') position++;
                }
            }
        }

        [TestMethod]
        public void WrapObjectNames_StopsAtTheLineLimit()
        {
            var nodes = WrappedNames(PlanNodeWidth.SuperCompact, maxLines: 2).Nodes;

            Assert.IsTrue(nodes.All(n => n.SubtitleLines.Count <= 2));
        }

        [TestMethod]
        public void ObjectNamesStayOnOneLineUnlessAskedToWrap()
        {
            var options = new PlanLayoutOptions();
            options.SetNodeWidth(PlanNodeWidth.SuperCompact);

            foreach (var node in Layout(TestPlans.ParallelSpill, options).Nodes.Where(n => n.Subtitle is not null))
            {
                CollectionAssert.AreEqual(new[] { node.Subtitle }, node.SubtitleLines.ToArray());
            }
        }

        [TestMethod]
        public void OnlyTheStackedPresetPutsTheIconAbove()
        {
            var options = new PlanLayoutOptions();

            foreach (var width in Enum.GetValues<PlanNodeWidth>())
            {
                options.SetNodeWidth(width);
                Assert.AreEqual(width == PlanNodeWidth.Stacked, options.IconAboveText, width.ToString());
            }
        }

        [TestMethod]
        public void ColumnSpacingPresets_GetWiderInOrderAndNormalIsTheDefault()
        {
            Assert.AreEqual(new PlanLayoutOptions().ColumnSpacing, PlanColumnSpacings.Spacing(PlanColumnSpacing.Normal));

            var spacings = Enum.GetValues<PlanColumnSpacing>().Select(PlanColumnSpacings.Spacing).ToList();
            for (var i = 1; i < spacings.Count; i++) Assert.IsTrue(spacings[i] > spacings[i - 1]);
        }

        [TestMethod]
        public void Layout_LeavesTheChosenSpacingBetweenColumns()
        {
            foreach (var spacing in Enum.GetValues<PlanColumnSpacing>())
            {
                var options = new PlanLayoutOptions();
                options.SetColumnSpacing(spacing);
                var expected = PlanColumnSpacings.Spacing(spacing);

                foreach (var node in Layout(TestPlans.ParallelSpill, options).Nodes.Where(n => n.Parent is not null))
                {
                    Assert.AreEqual(expected, node.Bounds.Left - node.Parent!.Bounds.Right, 0.001,
                        $"Gap before {node.Title} at {spacing}.");
                }
            }
        }

        [TestMethod]
        public void Relayout_PicksUpChangedOptionsAndKeepsTheCollapsedSet()
        {
            var options = new PlanLayoutOptions();
            var layout = Layout(TestPlans.ParallelSpill, options);
            var collapsible = layout.Nodes.First(n => !n.IsRoot && n.Children.Count > 0);
            Assert.IsTrue(layout.ToggleCollapse(collapsible));
            var before = layout.Bounds.Width;

            options.SetNodeWidth(PlanNodeWidth.Compact);
            options.MaxNodeWidth = options.MinNodeWidth;
            layout.Relayout();

            Assert.IsTrue(layout.Bounds.Width < before, "Narrower nodes should make a narrower plan.");
            Assert.IsTrue(layout.Nodes.All(n => n.Bounds.Width == options.MinNodeWidth));
            Assert.IsTrue(layout.HasCollapsedNodes, "Laying out again keeps what was collapsed.");
        }

        [TestMethod]
        public void Controller_RelayoutKeepsTheSelection()
        {
            var options = new PlanLayoutOptions();
            var layout = Layout(TestPlans.ParallelSpill, options);
            var controller = new PlanViewController(layout);
            controller.SetViewport(new LayoutSize(800, 600));
            controller.Select(layout.Nodes.Single(n => n.Operator?.NodeId == 1));

            options.SetNodeWidth(PlanNodeWidth.Widest);
            controller.Relayout();

            Assert.AreEqual(1, controller.SelectedNode!.Operator!.NodeId);
            Assert.IsTrue(layout.Nodes.Contains(controller.SelectedNode), "The selection should be one of the new nodes.");
        }

        [TestMethod]
        public void Layout_CentresAConsumerOnItsInputs()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);

            var join = layout.Nodes.Single(n => n.Kind == PlanOperatorKind.NestedLoops);
            Assert.AreEqual(2, join.Children.Count);

            var expected = (join.Children[0].Bounds.Centre.Y + join.Children[1].Bounds.Centre.Y) / 2;
            Assert.AreEqual(expected, join.Bounds.Centre.Y, 0.001);
        }

        [TestMethod]
        public void Layout_LeavesNoTwoNodesOverlappingInAColumn()
        {
            var layout = Layout(TestPlans.ParallelSpill);

            foreach (var column in layout.Nodes.GroupBy(n => n.Depth))
            {
                var ordered = column.OrderBy(n => n.Bounds.Top).ToList();

                for (var i = 1; i < ordered.Count; i++)
                {
                    Assert.IsTrue(
                        ordered[i].Bounds.Top >= ordered[i - 1].Bounds.Bottom,
                        $"'{ordered[i].Title}' overlaps '{ordered[i - 1].Title}' in column {column.Key}.");
                }
            }
        }

        [TestMethod]
        public void Layout_StartsAtTheOriginWithAMargin()
        {
            var options = new PlanLayoutOptions { Margin = 30 };
            var layout = Layout(TestPlans.KeyLookupSeek, options);

            Assert.AreEqual(0, layout.Bounds.X);
            Assert.AreEqual(0, layout.Bounds.Y);
            Assert.IsTrue(layout.Nodes.All(n => n.Bounds.Left >= options.Margin - 8));
            Assert.IsTrue(layout.Nodes.All(n => n.Bounds.Top >= options.Margin));
            Assert.IsTrue(layout.Nodes.All(n => n.Bounds.Right <= layout.Bounds.Right));
            Assert.IsTrue(layout.Nodes.All(n => n.Bounds.Bottom <= layout.Bounds.Bottom));
        }

        [TestMethod]
        public void Layout_PlacesTheNodeContentsInsideTheNode()
        {
            var layout = Layout(TestPlans.ParallelSpill);

            foreach (var node in layout.Nodes)
            {
                // The renderer draws exactly where these say, so anything outside the node would be
                // text over an arrow.
                Assert.IsTrue(node.Bounds.Contains(node.IconBounds.Centre), node.Title);
                Assert.IsTrue(node.TitleBounds.Left >= node.IconBounds.Right, node.Title);
                Assert.IsTrue(node.TitleBounds.Right <= node.Bounds.Right, node.Title);

                if (node.MetricBarBounds is { } bar)
                {
                    Assert.AreEqual(node.Bounds.Bottom, bar.Bottom, 0.001);
                    Assert.AreEqual(node.Bounds.Width, bar.Width, 0.001);
                }
            }
        }

        [TestMethod]
        public void Layout_RunsTheFiguresUnderTheGlyph()
        {
            var options = new PlanLayoutOptions();
            var layout = Layout(TestPlans.ParallelSpill, options);

            foreach (var node in layout.Nodes.Where(n => n.MetricBounds is not null))
            {
                // The title sits beside the glyph, but the figures - the longest lines on an actual
                // plan - start at the node's own edge rather than being indented past the glyph too.
                var metric = node.MetricBounds!.Value;
                Assert.AreEqual(node.Bounds.Left + options.NodePadding, metric.Left, 0.001, node.Title);
                Assert.AreEqual(node.Bounds.Right - options.NodePadding, metric.Right, 0.001, node.Title);
                Assert.IsTrue(metric.Top >= node.IconBounds.Bottom, node.Title);
            }
        }

        [TestMethod]
        public void Layout_SizesANodeToItsLongestLineWithoutTheGlyph()
        {
            var measurer = new FakeTextMeasurer();
            var options = new PlanLayoutOptions { MinNodeWidth = 0, UniformColumnWidths = false };
            var layout = new PlanLayoutEngine(measurer, options).Layout(TestPlans.Statement(TestPlans.ParallelSpill));

            // The root's timing line is its longest, and the node is exactly that plus padding: the
            // glyph's width is only paid on the heading, beside the title.
            var root = layout.Root;
            var timing = measurer.Measure(root.TimingLine!, PlanTextRole.Metric).Width;
            var heading = options.IconSize + options.IconSpacing + measurer.Measure(root.Subtitle!, PlanTextRole.Detail).Width;
            Assert.IsTrue(timing > heading, "The fixture should have a timing line longer than the heading.");
            Assert.AreEqual(timing + (options.NodePadding * 2), root.Bounds.Width, 0.001);
        }

        [TestMethod]
        public void Layout_PutsBadgesOnTheTopEdgeWhereTheyCostTheTitleNothing()
        {
            var options = new PlanLayoutOptions();
            var layout = Layout(TestPlans.ParallelSpill, options);

            var badged = layout.Nodes.First(n => PlanBadges.Count(n.Badges) > 0);
            var strip = badged.BadgeStripBounds;

            Assert.IsNotNull(strip, "A node with badges needs a strip to draw them in.");

            // Across the top border, inside the node's width, and clear of the title - which gets
            // the full width of the heading rather than stopping short of the badges.
            Assert.IsTrue(strip!.Value.Top < badged.Bounds.Top && strip.Value.Bottom > badged.Bounds.Top);
            Assert.IsTrue(strip.Value.Left > badged.Bounds.Left && strip.Value.Right < badged.Bounds.Right);
            Assert.IsFalse(strip.Value.IntersectsWith(badged.TitleBounds), "The badges must not sit on the title.");
            Assert.AreEqual(badged.Bounds.Right - options.NodePadding, badged.TitleBounds.Right, 0.001);

            // ...and a node with none has no strip.
            var plain = layout.Nodes.First(n => n.Badges == PlanNodeBadges.None);
            Assert.IsNull(plain.BadgeStripBounds);
        }

        [TestMethod]
        public void Layout_ShowsElapsedAndCpuTimeOnAnActualPlan()
        {
            var layout = Layout(TestPlans.ParallelSpill);

            // The sort's own time by default: its busiest thread's 900 ms less the 330 ms the batch
            // hash join and both scans took on that thread, and its CPU less theirs likewise.
            var sort = layout.Nodes.Single(n => n.Operator?.NodeId == 1);
            Assert.AreEqual("Elapsed 570 ms · CPU 530 ms", sort.TimingLine);

            // The statement's own totals go on the root, labelled the same way.
            Assert.AreEqual("Elapsed 1.50 s · CPU 3.20 s", layout.Root.TimingLine);
            Assert.AreEqual("Cost 100", layout.Root.MetricLine);

            // Drawn below the metrics, inside the node.
            Assert.IsNotNull(sort.TimingBounds);
            Assert.IsTrue(sort.TimingBounds!.Value.Top >= sort.MetricBounds!.Value.Bottom);
            Assert.IsTrue(sort.TimingBounds.Value.Bottom <= sort.MetricBarBounds!.Value.Top);
        }

        [TestMethod]
        public void Edges_AreDrawnByActualRowsByDefault()
        {
            var layout = Layout(TestPlans.ParallelSpill);
            var scan = EdgeFrom(layout, 3);

            // The scan was estimated at 100 rows and produced 2,500 on each of four threads.
            Assert.AreEqual(PlanEdgeWidthBasis.Actual, layout.EffectiveEdgeWidthBasis);
            Assert.AreEqual(10_000, scan.Rows);
            Assert.AreEqual("10K", scan.Label);
            Assert.IsTrue(scan.IsActual);
        }

        [TestMethod]
        public void SetEdgeWidthBasis_DrawsTheEstimatesOnTheSameScale()
        {
            var layout = Layout(TestPlans.ParallelSpill);
            var node = layout.Nodes.Single(n => n.Operator?.NodeId == 3);
            var actualScan = EdgeFrom(layout, 3);
            var actualSort = EdgeFrom(layout, 1);

            Assert.IsFalse(layout.SetEdgeWidthBasis(PlanEdgeWidthBasis.Actual), "Already drawn by actuals.");
            Assert.IsTrue(layout.SetEdgeWidthBasis(PlanEdgeWidthBasis.Estimated));

            var estimatedScan = EdgeFrom(layout, 3);
            Assert.AreEqual(100, estimatedScan.Rows);
            Assert.AreEqual("100", estimatedScan.Label);
            Assert.IsFalse(estimatedScan.IsActual, "Drawn in the estimated colour, since it shows an estimate.");

            // One scale for both, so the arrow that was badly estimated changes width...
            Assert.IsTrue(estimatedScan.Thickness < actualScan.Thickness);

            // ...and the one estimated exactly, at 1,000 rows either way, does not.
            Assert.AreEqual(actualSort.Thickness, EdgeFrom(layout, 1).Thickness, 0.0001);

            // Only the arrows are routed again: the nodes are the same objects in the same places.
            Assert.AreSame(node, layout.Nodes.Single(n => n.Operator?.NodeId == 3));
        }

        [TestMethod]
        public void Edges_OnAnEstimatedPlanAreDrawnByTheEstimateWhateverIsChosen()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);

            // Actual was asked for, but there is nothing measured to draw by.  The choice is kept for
            // the next actual plan; what is drawn is the estimate.
            Assert.AreEqual(PlanEdgeWidthBasis.Actual, layout.EdgeWidthBasis);
            Assert.AreEqual(PlanEdgeWidthBasis.Estimated, layout.EffectiveEdgeWidthBasis);
            Assert.IsFalse(layout.Edges.Any(e => e.IsActual));
        }

        [TestMethod]
        public void Edges_CarryTheEstimateForEveryExpectedExecution()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);

            // The key lookup is estimated at one row a time, run once plus nine rebinds.  An arrow of
            // one row would read as a hairline beside an actual ten.
            var lookup = TestPlans.Operator(layout.Statement, 2);
            Assert.AreEqual(10, lookup.EstimatedExecutions);
            Assert.AreEqual(10, lookup.EstimatedTotalRows);
            Assert.AreEqual(10, EdgeFrom(layout, 2).Rows);

            // The tooltip says where the ten came from.
            var tooltip = PlanTooltipBuilder.Build(layout.Nodes.Single(n => n.Operator?.NodeId == 2));
            Assert.AreEqual("1", tooltip.Rows.Single(r => r.Label == "Estimated rows").Value);
            Assert.AreEqual("10", tooltip.Rows.Single(r => r.Label == "Estimated executions").Value);
            Assert.AreEqual("10", tooltip.Rows.Single(r => r.Label == "Estimated rows, all executions").Value);
        }

        private static PlanEdge EdgeFrom(PlanLayout layout, int nodeId) =>
            layout.Edges.Single(e => e.From.Operator?.NodeId == nodeId);

        [TestMethod]
        public void Layout_ShowsTheTimesAsReportedWhenAskedTo()
        {
            var layout = Layout(TestPlans.ParallelSpill, new PlanLayoutOptions { OperatorTimeMode = OperatorTimeMode.AsReported });

            // What SSMS shows: the sort's slowest thread took 900 ms and its threads burned 1,280 ms
            // of CPU between them, inputs included.  Shortened for the node.
            var sort = layout.Nodes.Single(n => n.Operator?.NodeId == 1);
            Assert.AreEqual("Elapsed 900 ms · CPU 1.28 s", sort.TimingLine);
        }

        [TestMethod]
        public void SetOperatorTimeMode_LaysThePlanOutAgainWithTheOtherTimes()
        {
            var layout = Layout(TestPlans.ParallelSpill);
            var before = layout.Nodes.Single(n => n.Operator?.NodeId == 1);

            Assert.IsFalse(layout.SetOperatorTimeMode(OperatorTimeMode.Own), "Already showing own time.");
            Assert.IsTrue(layout.SetOperatorTimeMode(OperatorTimeMode.AsReported));

            var after = layout.Nodes.Single(n => n.Operator?.NodeId == 1);
            Assert.AreEqual("Elapsed 900 ms · CPU 1.28 s", after.TimingLine);
            Assert.IsFalse(ReferenceEquals(before, after), "The times are node text, so the nodes are rebuilt.");
            Assert.AreEqual(OperatorTimeMode.AsReported, layout.Metrics.TimeMode);
        }

        [TestMethod]
        public void Metrics_RankTimeByTheChosenMode()
        {
            var own = Layout(TestPlans.ParallelSpill);
            var reported = Layout(TestPlans.ParallelSpill, new PlanLayoutOptions { OperatorTimeMode = OperatorTimeMode.AsReported });

            var scan = TestPlans.Operator(own.Statement, 3);

            // As reported, the exchange at the root contains the whole plan's time, and every bar is
            // measured against a total that includes the operator itself.
            Assert.AreEqual(1500, reported.Metrics.MaxElapsedMs);

            // By own time the scale is the most any one operator took itself, so the scan's 90 ms
            // counts for more of it.
            Assert.AreEqual(600, own.Metrics.MaxElapsedMs);
            Assert.IsTrue(
                own.Metrics.Fraction(scan, PlanHeatMetric.Elapsed) > reported.Metrics.Fraction(scan, PlanHeatMetric.Elapsed));
        }

        [TestMethod]
        public void Layout_ShowsNoTimingsOnAnEstimatedPlan()
        {
            // Nothing was measured, so there is nothing to show - rather than a line of zeroes.
            var layout = Layout(TestPlans.KeyLookupSeek);

            Assert.IsTrue(layout.Nodes.All(n => n.TimingLine is null && n.TimingBounds is null));
        }

        [TestMethod]
        public void Layout_GivesTheRootNoMetricBar()
        {
            // Its share of anything is always everything, so a permanently full bar says nothing.
            Assert.IsNull(Layout(TestPlans.KeyLookupSeek).Root.MetricBarBounds);
        }

        [TestMethod]
        public void Layout_BadgesTheThingsWorthSpotting()
        {
            var layout = Layout(TestPlans.ParallelSpill);

            var sort = layout.Nodes.Single(n => n.Operator?.NodeId == 1);
            Assert.IsTrue(sort.Badges.HasFlag(PlanNodeBadges.CriticalWarning), "The sort spilled.");
            Assert.IsTrue(sort.Badges.HasFlag(PlanNodeBadges.Parallel));

            var scan = layout.Nodes.Single(n => n.Operator?.NodeId == 3);
            Assert.IsTrue(scan.Badges.HasFlag(PlanNodeBadges.EstimateMismatch), "100x under-estimate.");
            Assert.IsTrue(scan.Badges.HasFlag(PlanNodeBadges.RowsDiscarded), "30,000 rows read and dropped.");

            var hash = layout.Nodes.Single(n => n.Operator?.NodeId == 2);
            Assert.IsTrue(hash.Badges.HasFlag(PlanNodeBadges.BatchMode));
        }

        [TestMethod]
        public void Layout_BadgesAnOperatorThatReturnedNothingAgainstALargeEstimate()
        {
            // The largest over-estimate a plan can hold: a hundred thousand rows expected and none
            // returned.  Actual over estimated is 0 there, which reads as no error at all, so the
            // badge, the heat metric and the statement list all measure it the way the arrow does -
            // clamped at one row either end - rather than passing over it.
            var statement = Parse(
                Op(0, "Top", "Top", 1, Rows(1),
                    Op(1, "Clustered Index Scan", "Clustered Index Scan", 100_000, Rows(0))));

            var layout = Layout(statement);
            var scan = layout.Nodes.Single(n => n.Operator?.NodeId == 1);

            Assert.IsTrue(scan.Badges.HasFlag(PlanNodeBadges.EstimateMismatch));
            Assert.AreEqual(100_000, scan.Operator!.RowEstimateError);

            // All three agree about which operator is the worst estimated, and by how much.
            Assert.IsTrue(layout.Metrics.Supports(PlanHeatMetric.EstimateError));
            Assert.AreEqual(1, layout.Metrics.Fraction(scan.Operator, PlanHeatMetric.EstimateError), 0.0001);
            Assert.AreEqual(
                PlanEstimateAccuracy.Critical,
                layout.Edges.Single(e => e.From.Operator?.NodeId == 1).EstimateAccuracy);
        }

        [TestMethod]
        public void Layout_BadgesAnOperatorThatReturnedRowsAgainstAnEstimateOfNone()
        {
            // The mirror of it, and null for the same reason: there is no ratio to an estimate of
            // zero, but ten thousand rows nobody expected is still an estimate a mile out.
            var statement = Parse(
                Op(0, "Top", "Top", 1, Rows(1),
                    Op(1, "Table Scan", "Table Scan", 0, Rows(10_000))));

            var scan = Layout(statement).Nodes.Single(n => n.Operator?.NodeId == 1);

            Assert.IsNull(scan.Operator!.RowEstimateRatio, "No ratio to a zero estimate.");
            Assert.AreEqual(10_000, scan.Operator.RowEstimateError);
            Assert.IsTrue(scan.Badges.HasFlag(PlanNodeBadges.EstimateMismatch));

            // Badged, so the tooltip has to say why - it has no ratio to show for it.
            var tooltip = PlanTooltipBuilder.Build(scan, int.MaxValue, OperatorTimeMode.Own);
            var estimate = tooltip.Rows.Single(r => r.Label == "Actual vs estimated");
            Assert.AreEqual("10000x more", estimate.Value);
            Assert.IsTrue(estimate.IsEmphasised);
        }

        [TestMethod]
        public void Layout_LeavesAWellEstimatedOperatorUnbadged()
        {
            // The clamping must not badge an operator that was estimated perfectly well - a badge
            // that appears on everything is one nobody reads.
            var statement = Parse(
                Op(0, "Top", "Top", 1, Rows(1),
                    Op(1, "Index Seek", "Index Seek", 0.3, Rows(1))));

            var seek = Layout(statement).Nodes.Single(n => n.Operator?.NodeId == 1);

            Assert.AreEqual(1, seek.Operator!.RowEstimateError, "0.3 rows expected, one returned.");
            Assert.IsFalse(seek.Badges.HasFlag(PlanNodeBadges.EstimateMismatch));
        }

        [TestMethod]
        public void Layout_MarksEveryOperatorWithWarningsOnItsGlyph()
        {
            var layout = Layout(TestPlans.ParallelSpill);

            foreach (var node in layout.Nodes.Where(n => n.Operator is not null))
            {
                var warned = node.Operator!.Warnings.Count > 0;
                Assert.AreEqual(warned, node.HasWarnings, node.Title);
                Assert.AreEqual(warned, node.WarningMarkerBounds is not null, node.Title);

                if (node.WarningMarkerBounds is not { } marker) continue;

                // Over the glyph's lower right corner, where SSMS puts it, and no lower than the
                // glyph's foot, so it never sits on the figures underneath.
                var corner = new LayoutPoint(node.IconBounds.Right - 1, node.IconBounds.Bottom - 1);
                Assert.IsTrue(marker.Contains(corner), node.Title);
                Assert.IsTrue(marker.Bottom <= node.IconBounds.Bottom + 0.001, node.Title);
                Assert.IsTrue(marker.Right < node.TitleBounds.Left, node.Title);
            }
        }

        [TestMethod]
        public void Layout_KeepsWarningsOutOfTheBadgeStrip()
        {
            // The triangle on the glyph is the warning marker.  The same warning drawn again in the
            // strip would be noise, and would widen the strip for nothing.
            var badges = PlanNodeBadges.Warning | PlanNodeBadges.CriticalWarning | PlanNodeBadges.Parallel;

            CollectionAssert.AreEqual(new[] { PlanNodeBadges.Parallel }, PlanBadges.Ordered(badges).ToArray());
            Assert.AreEqual(0, PlanBadges.Count(PlanNodeBadges.Warning | PlanNodeBadges.CriticalWarning));
        }

        [TestMethod]
        public void Layout_MarksTheStatementWithPlanLevelWarnings()
        {
            // A cross join flagged on the statement rather than on any operator.
            var statement = TestPlans.Load(TestPlans.Batch).Statements.Single(s => s.Warnings.Count > 0);
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(statement);

            Assert.IsTrue(layout.Root.HasWarnings);
            Assert.IsTrue(layout.Root.Badges.HasFlag(PlanNodeBadges.CriticalWarning), "No join predicate is critical.");
            Assert.IsNotNull(layout.Root.WarningMarkerBounds);

            // ...and a statement with none leaves the root unmarked.
            Assert.IsFalse(Layout(TestPlans.ParallelSpill).Root.HasWarnings);
        }

        [TestMethod]
        public void Layout_RaisesHiddenWarningsOnACollapsedNode()
        {
            var layout = Layout(TestPlans.ParallelSpill);

            // The exchange has no warnings of its own, but the sort and the hash join under it
            // both spilled.  Collapsing it must not make the spills disappear from the picture.
            var exchange = layout.Nodes.Single(n => n.Operator?.NodeId == 0);
            Assert.IsFalse(exchange.HasWarnings);

            layout.ToggleCollapse(exchange);
            var collapsed = layout.Nodes.Single(n => n.Operator?.NodeId == 0);

            Assert.IsTrue(collapsed.HasWarnings);
            Assert.IsTrue(collapsed.Badges.HasFlag(PlanNodeBadges.CriticalWarning));
            Assert.IsNotNull(collapsed.WarningMarkerBounds);
            Assert.AreEqual(2, collapsed.HiddenWarningCount);

            // The tooltip says whose warnings they are, since they are not the exchange's own.
            var tooltip = PlanTooltipBuilder.Build(collapsed);
            Assert.IsTrue(tooltip.Rows.Any(r => r.Label == "Hidden warnings" && r.Value.StartsWith("2 ", StringComparison.Ordinal)));

            // Expanded again, the marker goes back to the operators it belongs to.
            layout.ExpandAll();
            Assert.IsFalse(layout.Nodes.Single(n => n.Operator?.NodeId == 0).HasWarnings);
        }

        [TestMethod]
        public void Layout_BadgesTheOperatorAMissingIndexIsAbout()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);

            // The recommendation is on the plan and names a table; the reader needs to know which
            // operator to look at.
            var onOrders = layout.Nodes.Where(n => n.Badges.HasFlag(PlanNodeBadges.MissingIndex)).ToList();

            Assert.IsTrue(onOrders.Count > 0, "The index is wanted on Sales.dbo.Orders.");
            Assert.IsTrue(onOrders.All(n =>
                n.Operator!.PrimaryObject!.QualifiedTableName == "Sales.dbo.Orders"));
        }

        [TestMethod]
        public void Layout_ScalesArrowThicknessWithRows()
        {
            var layout = Layout(TestPlans.ParallelSpill);

            var fat = layout.Edges.Single(e => e.From.Operator?.NodeId == 3);
            var thin = layout.Edges.Single(e => e.From.Operator?.NodeId == 4);

            // 10,000 rows against 1,000, so the first arrow has to be visibly heavier.
            Assert.IsTrue(fat.Rows > thin.Rows);
            Assert.IsTrue(fat.Thickness > thin.Thickness);
            Assert.IsTrue(layout.Edges.All(e => e.Thickness >= 1.5 && e.Thickness <= 22));
        }

        [TestMethod]
        public void EdgeWidth_ByRows_StaysThinForASmallPlan()
        {
            // The busiest arrow in this plan carries ten rows.  Without the floor it would be drawn
            // at full width, and a ten row query would look as heavy as a ten million row one.
            var options = new PlanLayoutOptions();
            var layout = Layout(TestPlans.KeyLookupSeek, options);

            Assert.AreEqual(10, layout.Metrics.MaxRows);
            Assert.IsTrue(
                layout.Edges.All(e => e.Thickness < options.MaxEdgeThickness / 2),
                "A ten row plan should not have wide arrows.");
        }

        [TestMethod]
        public void EdgeWidth_ByRows_KeepsAPlanOfThousandsOfRowsLight()
        {
            // The busiest arrow carries 10,000 rows: 1% of the million row floor.  On the square
            // root scale that is a tenth of the width range - where the log scale this replaced drew
            // it at two thirds, and a plan of a few thousand rows came out in thick pipes.
            var options = new PlanLayoutOptions();
            var layout = Layout(TestPlans.ParallelSpill, options);

            var busiest = layout.Edges.Single(e => e.Rows == 10000);
            var expected = options.MinEdgeThickness + (0.1 * (options.MaxEdgeThickness - options.MinEdgeThickness));

            Assert.AreEqual(expected, busiest.Thickness, 0.0001);
        }

        [TestMethod]
        public void EdgeWidth_ByRows_ScalesAgainstThePlanOnceItPassesTheFloor()
        {
            // Past the floor, the busiest arrow in the plan is full width and the rest are relative
            // to it - the widths compare arrows within this plan, not against a fixed yardstick.
            var options = new PlanLayoutOptions { EdgeWidthRowsFloor = 1000 };
            var layout = Layout(TestPlans.ParallelSpill, options);

            Assert.AreEqual(10000, layout.Metrics.MaxRows);

            var busiest = layout.Edges.Single(e => e.Rows == 10000);
            Assert.AreEqual(options.MaxEdgeThickness, busiest.Thickness, 0.0001);
            Assert.IsTrue(layout.Edges.Where(e => e != busiest).All(e => e.Thickness < busiest.Thickness));
        }

        [TestMethod]
        public void EdgeWidth_WithTheFloorOff_GivesTheBusiestArrowFullWidth()
        {
            var options = new PlanLayoutOptions { EdgeWidthRowsFloor = 0 };
            var layout = Layout(TestPlans.KeyLookupSeek, options);

            Assert.AreEqual(options.MaxEdgeThickness, layout.Edges.Max(e => e.Thickness), 0.0001);
        }

        [TestMethod]
        public void EdgeWidth_ByDataSize_WeighsRowsByTheirSize()
        {
            var options = new PlanLayoutOptions { EdgeWidthRowsFloor = 0, EdgeWidthDataSizeFloor = 0 };
            var layout = Layout(TestPlans.ParallelSpill, options);

            // The hash join and the table scan both hand on a thousand rows, but the join's rows are
            // forty bytes and the scan's twenty five.  By rows they look the same; by data size the
            // join is the heavier arrow, which is the point of offering the choice.
            PlanEdge From(int nodeId) => layout.Edges.Single(e => e.From.Operator?.NodeId == nodeId);

            Assert.AreEqual(From(2).Thickness, From(4).Thickness, 0.0001);

            layout.SetEdgeWidthMetric(PlanEdgeWidthMetric.DataSize);

            Assert.AreEqual(40_000, From(2).DataSize, 0.0001);
            Assert.AreEqual(25_000, From(4).DataSize, 0.0001);
            Assert.IsTrue(From(2).Thickness > From(4).Thickness);
        }

        [TestMethod]
        public void EdgeWidth_LabelsSayWhatTheWidthIsMeasuring()
        {
            var layout = Layout(TestPlans.ParallelSpill);
            var scan = layout.Edges.Single(e => e.From.Operator?.NodeId == 3);

            Assert.AreEqual("10K", scan.Label);

            layout.SetEdgeWidthMetric(PlanEdgeWidthMetric.DataSize);
            scan = layout.Edges.Single(e => e.From.Operator?.NodeId == 3);

            // 10,000 rows of 30 bytes.
            Assert.AreEqual("293 KB", scan.Label);
        }

        [TestMethod]
        public void EdgeWidth_ByDataSize_HasItsOwnFloor()
        {
            // The busiest arrow carries 300 KB, well under the default 100 MB floor, so it must not
            // be drawn at full width - and with the floor off, it must.
            var options = new PlanLayoutOptions { EdgeWidthMetric = PlanEdgeWidthMetric.DataSize };
            var floored = Layout(TestPlans.ParallelSpill, options);

            Assert.AreEqual(PlanEdgeWidthMetric.DataSize, floored.EdgeWidthMetric, "Taken from the options.");
            Assert.IsTrue(floored.Edges.All(e => e.Thickness < options.MaxEdgeThickness * 0.75));

            options.EdgeWidthDataSizeFloor = 0;
            var unfloored = Layout(TestPlans.ParallelSpill, options);
            Assert.AreEqual(options.MaxEdgeThickness, unfloored.Edges.Max(e => e.Thickness), 0.0001);
        }

        [TestMethod]
        public void EdgeWidth_ATypicalRowWeighsTheSameUnderEitherMeasure()
        {
            // Both floors describe the same small plan - a million rows, or a million hundred byte
            // rows - so an arrow of hundred byte rows should be the same width either way, and
            // switching measure should only move the arrows whose rows are unusually wide or narrow.
            var byRows = Layout(TestPlans.ParallelSpill, new PlanLayoutOptions { EdgeWidthRowsFloor = 1_000_000 });
            var byBytes = Layout(TestPlans.ParallelSpill, new PlanLayoutOptions
            {
                EdgeWidthMetric = PlanEdgeWidthMetric.DataSize,
                EdgeWidthDataSizeFloor = 100_000_000
            });

            // The scan's rows are 30 bytes: 10,000 of them weigh what 3,000 typical rows would.
            var scanByBytes = byBytes.Edges.Single(e => e.From.Operator?.NodeId == 3).Thickness;
            var sortByRows = byRows.Edges.Single(e => e.From.Operator?.NodeId == 1).Thickness;
            var scanByRows = byRows.Edges.Single(e => e.From.Operator?.NodeId == 3).Thickness;

            // 3,000 row-equivalents sits between the sort's 1,000 rows and the scan's own 10,000.
            Assert.IsTrue(scanByBytes > sortByRows);
            Assert.IsTrue(scanByBytes < scanByRows);
        }

        [TestMethod]
        public void SetEdgeWidthMetric_ReroutesTheArrowsWithoutReplacingTheNodes()
        {
            var layout = Layout(TestPlans.ParallelSpill);
            var nodes = layout.Nodes;
            var edges = layout.Edges;

            Assert.IsTrue(layout.SetEdgeWidthMetric(PlanEdgeWidthMetric.DataSize));

            // Nodes are the same objects, so anything holding one - a selection, a search result -
            // is still valid.  Only the arrows were rebuilt.
            Assert.AreSame(nodes, layout.Nodes);
            Assert.AreNotSame(edges, layout.Edges);

            Assert.IsFalse(layout.SetEdgeWidthMetric(PlanEdgeWidthMetric.DataSize), "Already by data size.");
        }

        [TestMethod]
        public void Layout_DrawsTheHeaviestArrowsFirst()
        {
            var layout = Layout(TestPlans.ParallelSpill);

            // Paint order, so a thin arrow crossing a thick one stays visible.
            var thicknesses = layout.Edges.Select(e => e.Thickness).ToList();
            CollectionAssert.AreEqual(thicknesses.OrderByDescending(t => t).ToList(), thicknesses);
        }

        [TestMethod]
        public void Layout_RoutesEachArrowFromItsProducerToItsConsumer()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);

            foreach (var edge in layout.Edges)
            {
                Assert.AreEqual(4, edge.Points.Count);
                Assert.AreEqual(edge.From.Bounds.Left, edge.Points[0].X, 0.001);
                Assert.AreEqual(edge.From.Bounds.Centre.Y, edge.Points[0].Y, 0.001);
                Assert.AreEqual(edge.To.Bounds.Right, edge.Points[^1].X, 0.001);

                // The corner sits in the gap between the columns, which is the routing channel.
                Assert.IsTrue(edge.Points[1].X > edge.To.Bounds.Right);
                Assert.IsTrue(edge.Points[1].X < edge.From.Bounds.Left);
            }
        }

        [TestMethod]
        public void Layout_SpreadsSeveralInputsDownTheConsumersEdge()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);

            var join = layout.Nodes.Single(n => n.Kind == PlanOperatorKind.NestedLoops);
            var entries = layout.Edges.Where(e => e.To == join).Select(e => e.Points[^1].Y).ToList();

            // At the thicknesses a busy plan produces, arrows sharing one entry point hide each
            // other completely.
            Assert.AreEqual(2, entries.Count);
            Assert.AreNotEqual(entries[0], entries[1]);
            Assert.IsTrue(entries.All(y => y > join.Bounds.Top && y < join.Bounds.Bottom));
        }

        [TestMethod]
        public void Layout_PutsALabelBelowTheLineWhenTheArrowTurnsUpwards()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);

            var join = layout.Nodes.Single(n => n.Kind == PlanOperatorKind.NestedLoops);
            var inputs = layout.Edges.Where(e => e.To == join).OrderBy(e => e.Points[0].Y).ToList();

            // The upper input turns down into the join, so the space above its line is clear.  The
            // lower one turns up, and its riser runs through the space above its line - so its label
            // goes underneath, where it cannot sit on the arrow.
            var upper = inputs[0];
            var lower = inputs[1];
            Assert.IsTrue(upper.Points[^1].Y > upper.Points[0].Y && lower.Points[^1].Y < lower.Points[0].Y);
            Assert.IsTrue(upper.LabelBounds.Bottom <= upper.Points[0].Y - (upper.Thickness / 2));
            Assert.IsTrue(lower.LabelBounds.Top >= lower.Points[0].Y + (lower.Thickness / 2));

            // Either way, beside the producer and clear of it.
            Assert.IsTrue(inputs.All(e => e.LabelBounds.Right < e.From.Bounds.Left));
        }

        [TestMethod]
        public void Layout_MarksArrowsCarryingMeasuredRows()
        {
            Assert.IsTrue(Layout(TestPlans.ParallelSpill).Edges.All(e => e.IsActual));
            Assert.IsTrue(Layout(TestPlans.KeyLookupSeek).Edges.All(e => !e.IsActual));
        }

        [TestMethod]
        public void Metrics_ScaleAgainstTheWorstOperatorRatherThanTheTotal()
        {
            var layout = Layout(TestPlans.ParallelSpill);
            var metrics = layout.Metrics;

            Assert.IsTrue(metrics.HasRuntime);
            Assert.IsTrue(metrics.Supports(PlanHeatMetric.Cpu));
            Assert.IsTrue(metrics.Supports(PlanHeatMetric.EstimateError));

            // The worst operator gets a full bar, which is the answer to "which is the expensive
            // one" - against the total, every bar but one would be a hairline.
            var dearest = layout.Statement.Operators.OrderByDescending(o => o.OperatorCost).First();
            Assert.AreEqual(1, metrics.Fraction(dearest, PlanHeatMetric.OperatorCost), 0.0001);

            Assert.IsTrue(layout.Statement.Operators
                .All(o => metrics.Fraction(o, PlanHeatMetric.OperatorCost) is >= 0 and <= 1));
        }

        [TestMethod]
        public void Metrics_DoNotOfferAMeasuredMetricOnAnEstimatedPlan()
        {
            var metrics = Layout(TestPlans.KeyLookupSeek).Metrics;

            Assert.IsFalse(metrics.HasRuntime);
            Assert.IsTrue(metrics.Supports(PlanHeatMetric.OperatorCost));
            Assert.IsFalse(metrics.Supports(PlanHeatMetric.Cpu), "There is no CPU to rank by.");
            Assert.IsFalse(metrics.Supports(PlanHeatMetric.Elapsed));
        }

        [TestMethod]
        public void ToggleCollapse_HidesTheInputsAndSaysHowMany()
        {
            var layout = Layout(TestPlans.ParallelSpill);
            var before = layout.Nodes.Count;

            var hash = layout.Nodes.Single(n => n.Operator?.NodeId == 2);
            Assert.IsTrue(layout.ToggleCollapse(hash));

            // The node objects are rebuilt, so the collapsed one has to be found again.
            var collapsed = layout.Nodes.Single(n => n.Operator?.NodeId == 2);
            Assert.IsTrue(collapsed.IsCollapsed);
            Assert.AreEqual(0, collapsed.Children.Count);
            Assert.AreEqual(2, collapsed.HiddenDescendantCount);
            Assert.AreEqual(before - 2, layout.Nodes.Count);
            Assert.IsTrue(layout.HasCollapsedNodes);
        }

        [TestMethod]
        public void ToggleCollapse_IsRefusedOnANodeWithNoInputs()
        {
            var layout = Layout(TestPlans.ParallelSpill);

            var leaf = layout.Nodes.Single(n => n.Operator?.NodeId == 4);
            Assert.IsFalse(layout.ToggleCollapse(leaf));
            Assert.IsFalse(layout.HasCollapsedNodes);
        }

        [TestMethod]
        public void ExpandAll_PutsEverythingBack()
        {
            var layout = Layout(TestPlans.ParallelSpill);
            var before = layout.Nodes.Count;

            layout.ToggleCollapse(layout.Nodes.Single(n => n.Operator?.NodeId == 2));
            Assert.IsTrue(layout.ExpandAll());

            Assert.AreEqual(before, layout.Nodes.Count);
            Assert.IsFalse(layout.HasCollapsedNodes);
            Assert.IsFalse(layout.ExpandAll(), "Nothing left to expand.");
        }

        [TestMethod]
        public void FindVisibleNode_LandsOnTheCollapsedAncestorOfAHiddenOperator()
        {
            var layout = Layout(TestPlans.ParallelSpill);
            var hidden = TestPlans.Operator(layout.Statement, 4);

            layout.ToggleCollapse(layout.Nodes.Single(n => n.Operator?.NodeId == 2));

            Assert.IsNull(layout.FindNode(hidden), "It is not in the picture any more.");

            // Selecting it has to land somewhere, and the node standing in for it is the only honest
            // answer.
            Assert.AreEqual(2, layout.FindVisibleNode(hidden)!.Operator!.NodeId);
        }

        [TestMethod]
        public void HitTest_FindsTheNodeUnderAPoint()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);
            var node = layout.Nodes[2];

            Assert.AreSame(node, layout.HitTest(node.Bounds.Centre));
            Assert.IsNull(layout.HitTest(new LayoutPoint(-100, -100)));
        }

        [TestMethod]
        public void Layout_ShowsACollapseControlOnlyWhereThereIsSomethingToHide()
        {
            var layout = Layout(TestPlans.KeyLookupSeek);

            Assert.IsNotNull(layout.Nodes.Single(n => n.Kind == PlanOperatorKind.NestedLoops).CollapseToggleBounds);
            Assert.IsNull(layout.Nodes.Single(n => n.Kind == PlanOperatorKind.KeyLookup).CollapseToggleBounds);
        }

        [TestMethod]
        public void Layout_DrawsAStatementWithNoPlanAsASingleNodeThatSaysSo()
        {
            // The conditional in the batch has its plan under its Condition element rather than as a
            // statement plan of its own, so it appears in the list with nothing to draw.
            var statement = TestPlans.Load(TestPlans.Batch).Statements.Single(s => !s.HasPlan);
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(statement);

            // Better than an empty picture, which reads as a bug.
            Assert.AreEqual(1, layout.Nodes.Count);
            Assert.AreEqual("No plan recorded", layout.Root.Subtitle);
            Assert.AreEqual(0, layout.Edges.Count);
            Assert.IsTrue(layout.Bounds.Width > 0);
        }

        [TestMethod]
        public void Layout_RejectsANullStatement()
        {
            var engine = new PlanLayoutEngine(new FakeTextMeasurer());
            Assert.ThrowsExactly<ArgumentNullException>(() => engine.Layout(null!));
        }
    }
}
