using System;
using System.Linq;
using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Test
{
    [TestClass]
    public class PlanViewControllerTests
    {
        private static PlanViewController Controller(
            string plan = TestPlans.ParallelSpill,
            PlanViewOptions? options = null,
            double width = 800,
            double height = 600)
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(plan));
            var controller = new PlanViewController(layout, options);
            controller.SetViewport(new LayoutSize(width, height));
            return controller;
        }

        [TestMethod]
        public void SetViewport_FitsThePlanWhileTheViewIsStillOurs()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(TestPlans.ParallelSpill));
            var controller = new PlanViewController(layout, new PlanViewOptions { MinAutoFitZoom = 0.1 });
            controller.SetViewport(new LayoutSize(800, 600));

            Assert.IsFalse(controller.IsViewUserAdjusted);
            Assert.IsTrue(controller.Zoom > 0);

            // The whole plan is in view.
            var visible = controller.VisibleBounds;
            Assert.IsTrue(visible.Width >= controller.Layout.Bounds.Width - 0.001);
        }

        [TestMethod]
        public void SetViewport_DoesNotZoomOutPastActualSizeByDefault()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(TestPlans.ParallelSpill));
            var controller = new PlanViewController(layout);

            // A window far too small for the plan: it opens at full size to be scrolled rather than
            // shrunk to a picture nobody can read.
            controller.SetViewport(new LayoutSize((int)(layout.Bounds.Width / 4), 600));

            Assert.AreEqual(1.0, controller.Zoom, 0.001);

            // The head of the plan is where the reader starts, so it is what is on screen - the plan
            // runs off the right hand edge to be scrolled to.
            Assert.AreEqual(layout.Bounds.X, controller.VisibleBounds.Left, 0.001);
            Assert.IsTrue(controller.VisibleBounds.Width < layout.Bounds.Width);
            Assert.IsTrue(controller.VisibleBounds.IntersectsWith(layout.Root.Bounds), "The root should be in view.");
        }

        [TestMethod]
        public void SetViewport_ShowsTheRootWhenThePlanIsTooTallToFit()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(TestPlans.ParallelSpill));
            var controller = new PlanViewController(layout);

            // A window far shorter than the plan.  The root sits level with the middle of everything
            // feeding it, so anchoring on the plan's top or its centre can leave the view on blank
            // margin with no operator on screen.
            controller.SetViewport(new LayoutSize(800, (int)(layout.Bounds.Height / 4)));

            Assert.AreEqual(1.0, controller.Zoom, 0.001);

            var visible = controller.VisibleBounds;
            Assert.IsTrue(visible.IntersectsWith(layout.Root.Bounds), "The root should be in view.");
            Assert.AreEqual(layout.Root.Bounds.Centre.Y, visible.Centre.Y, 0.001, "Centred on the root.");

            // And never past the plan's own edges.
            Assert.IsTrue(visible.Top >= layout.Bounds.Top - 0.001 && visible.Bottom <= layout.Bounds.Bottom + 0.001);
        }

        [TestMethod]
        public void SetViewport_StillZoomsInToFillASmallPlansWindow()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(TestPlans.KeyLookupSeek));
            var controller = new PlanViewController(layout);

            controller.SetViewport(new LayoutSize((int)(layout.Bounds.Width * 4), (int)(layout.Bounds.Height * 4)));

            // Up to the cap on fitting, not four times over.
            Assert.AreEqual(1.25, controller.Zoom, 0.001);
        }

        [TestMethod]
        public void ZoomToFit_FitsTheWholePlanWhateverTheFloorSays()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(TestPlans.ParallelSpill));
            var controller = new PlanViewController(layout);
            controller.SetViewport(new LayoutSize((int)(layout.Bounds.Width / 4), 600));

            Assert.IsTrue(controller.ZoomToFit());

            Assert.IsTrue(controller.Zoom < 1, "The Fit button is not floored.");
            Assert.IsTrue(controller.VisibleBounds.Width >= layout.Bounds.Width - 0.001);

            // Fitting hands the view back, so resizing follows the window again.
            Assert.IsFalse(controller.IsViewUserAdjusted);
        }

        [TestMethod]
        public void AutoFit_LeavesAViewTheUserSetAlone()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(TestPlans.ParallelSpill));
            var controller = new PlanViewController(layout);
            controller.SetViewport(new LayoutSize(800, 600));
            controller.ZoomIn(new LayoutPoint(400, 300));

            var zoom = controller.Zoom;

            Assert.IsFalse(controller.AutoFit());
            Assert.AreEqual(zoom, controller.Zoom);
        }

        [TestMethod]
        public void FitAsOpened_RefitsOverAViewTheUserSet()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(TestPlans.ParallelSpill));
            var controller = new PlanViewController(layout, new PlanViewOptions { MinAutoFitZoom = 0.1 });
            controller.SetViewport(new LayoutSize(800, 600));

            var opened = controller.SaveView();
            controller.ZoomIn(new LayoutPoint(400, 300));
            Assert.AreNotEqual(opened, controller.SaveView());

            // Where opening the plan left it, which AutoFit would by now refuse to go back to - for
            // a change of shape, holding the old view is what loses the plan, not what saves it.
            Assert.IsTrue(controller.FitAsOpened());
            Assert.AreEqual(opened, controller.SaveView());
            Assert.IsFalse(controller.IsViewUserAdjusted);
        }

        [TestMethod]
        public void SetViewport_LeavesTheViewAloneOnceTheUserHasTouchedIt()
        {
            var controller = Controller();
            controller.ZoomIn(new LayoutPoint(400, 300));

            var zoom = controller.Zoom;
            Assert.IsTrue(controller.IsViewUserAdjusted);

            controller.SetViewport(new LayoutSize(1200, 900));

            // Re-fitting here would throw away whatever they had zoomed in to look at.
            Assert.AreEqual(zoom, controller.Zoom);
        }

        [TestMethod]
        public void SetViewport_WithoutRefit_KeepsTheViewForAPanelOpeningBesideIt()
        {
            var controller = Controller();
            var zoom = controller.Zoom;
            var pan = controller.Pan;

            // A properties panel opening takes width from the plan.  Re-fitting would rescale the
            // plan under the pointer that just clicked it; the view stays put instead.
            Assert.IsTrue(controller.SetViewport(new LayoutSize(460, 600), refit: false));

            Assert.AreEqual(zoom, controller.Zoom);
            Assert.AreEqual(pan, controller.Pan);
            Assert.AreEqual(new LayoutSize(460, 600), controller.Viewport);

            // Still the view we chose, so a real window resize afterwards fits again.
            Assert.IsFalse(controller.IsViewUserAdjusted);
        }

        [TestMethod]
        public void ScrollIntoView_MovesJustFarEnoughToUncoverANode()
        {
            var controller = Controller(width: 800, height: 600);
            controller.SetZoom(1, new LayoutPoint(0, 0));

            // The rightmost node, then the viewport narrowed until it is partly covered.
            var node = controller.Layout.Nodes.OrderByDescending(n => n.Bounds.Right).First();
            var screen = controller.ToScreen(node.Bounds);
            controller.SetViewport(new LayoutSize(screen.Right - 20, 600), refit: false);

            Assert.IsTrue(controller.ScrollIntoView(node, margin: 16));

            // Right edge now exactly the margin inside the viewport - not centred, which would throw
            // the rest of the plan across the screen to uncover twenty pixels.
            var after = controller.ToScreen(node.Bounds);
            Assert.AreEqual(controller.Viewport.Width - 16, after.Right, 0.001);
            Assert.AreEqual(screen.Top, after.Top, 0.001);
            Assert.IsTrue(controller.IsViewUserAdjusted, "A resize must not snap the view back over it.");
        }

        [TestMethod]
        public void RestoreView_PutsBackTheViewAndWhetherItWasStillFitted()
        {
            var controller = Controller();
            var saved = controller.SaveView();
            Assert.IsFalse(saved.IsUserAdjusted);

            controller.PanBy(-300, 0);
            Assert.IsTrue(controller.IsViewUserAdjusted);

            Assert.IsTrue(controller.RestoreView(saved));
            Assert.AreEqual(saved, controller.SaveView());

            // Fitted again, so resizing the window goes back to re-fitting it.
            Assert.IsFalse(controller.IsViewUserAdjusted);
        }

        [TestMethod]
        public void ScrollIntoView_LeavesTheViewAloneForANodeAlreadyInView()
        {
            var controller = Controller();
            var pan = controller.Pan;

            Assert.IsFalse(controller.ScrollIntoView(controller.Layout.Root));
            Assert.AreEqual(pan, controller.Pan);
        }

        [TestMethod]
        public void ScrollIntoView_ShowsTheStartOfANodeTooWideToFit()
        {
            var controller = Controller(width: 200, height: 600);
            controller.SetZoom(4, new LayoutPoint(0, 0));

            var node = controller.Layout.Nodes.First(n => n.Operator is not null);
            controller.ScrollIntoView(node, margin: 16);

            // The left of a node is its glyph and its name, which is what identifies it.
            Assert.AreEqual(16, controller.ToScreen(node.Bounds).Left, 0.001);
        }

        [TestMethod]
        public void ZoomToFit_HandsTheViewBackSoResizingFollowsTheWindowAgain()
        {
            var controller = Controller();
            controller.PanBy(50, 50);
            Assert.IsTrue(controller.IsViewUserAdjusted);

            controller.ZoomToFit();
            Assert.IsFalse(controller.IsViewUserAdjusted);
        }

        [TestMethod]
        public void ZoomToFit_PinsThePlansHeadToTheLeftEdge()
        {
            var controller = Controller();

            // A plan that does not fit should run off the right hand edge with its root still on
            // screen, not be cropped at both ends.
            Assert.AreEqual(0, controller.ToScreen(controller.Layout.Bounds).Left, 0.001);
        }

        [TestMethod]
        public void SetZoom_KeepsWhateverIsUnderThePointerInPlace()
        {
            var controller = Controller();
            var anchor = new LayoutPoint(300, 200);
            var before = controller.ToLayout(anchor);

            controller.SetZoom(controller.Zoom * 2.5, anchor);

            var after = controller.ToLayout(anchor);
            Assert.AreEqual(before.X, after.X, 0.001);
            Assert.AreEqual(before.Y, after.Y, 0.001);
        }

        [TestMethod]
        public void SetZoom_StaysWithinItsLimits()
        {
            var controller = Controller(options: new PlanViewOptions { MinZoom = 0.5, MaxZoom = 2 });
            var anchor = new LayoutPoint(0, 0);

            controller.SetZoom(100, anchor);
            Assert.AreEqual(2, controller.Zoom);

            controller.SetZoom(0.0001, anchor);
            Assert.AreEqual(0.5, controller.Zoom);
        }

        [TestMethod]
        public void PanTo_TracksThePointerRatherThanAcceleratingAwayFromIt()
        {
            var controller = Controller();
            var start = controller.Pan;

            controller.BeginPan(new LayoutPoint(100, 100));
            controller.PanTo(new LayoutPoint(140, 130));
            controller.PanTo(new LayoutPoint(160, 150));
            controller.EndPan();

            // The total movement is the total pointer movement, not the sum of the distances from a
            // fixed anchor.
            Assert.AreEqual(start.X + 60, controller.Pan.X, 0.001);
            Assert.AreEqual(start.Y + 50, controller.Pan.Y, 0.001);

            Assert.IsFalse(controller.PanTo(new LayoutPoint(200, 200)), "Panning has ended.");
        }

        [TestMethod]
        public void RoundTrippingAPointThroughBothTransformsReturnsIt()
        {
            var controller = Controller();
            controller.SetZoom(1.7, new LayoutPoint(123, 45));

            var point = new LayoutPoint(321, 654);
            var round = controller.ToLayout(controller.ToScreen(point));

            Assert.AreEqual(point.X, round.X, 0.0001);
            Assert.AreEqual(point.Y, round.Y, 0.0001);
        }

        [TestMethod]
        public void HitTest_FindsANodeThroughTheViewTransform()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[2];

            Assert.AreSame(node, controller.HitTest(controller.ToScreen(node.Bounds.Centre)));
        }

        [TestMethod]
        public void Select_RaisesOnlyWhenTheSelectionActuallyChanges()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[1];
            var raised = 0;
            controller.SelectionChanged += (_, _) => raised++;

            Assert.IsTrue(controller.Select(node));
            Assert.IsFalse(controller.Select(node), "Selecting the same node again changes nothing.");
            Assert.AreEqual(1, raised);

            Assert.IsTrue(controller.ClearSelection());
            Assert.IsNull(controller.SelectedNode);
            Assert.AreEqual(2, raised);
        }

        [TestMethod]
        public void Select_BuildsThePathBackToTheRoot()
        {
            var controller = Controller();
            var leaf = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 4);

            controller.Select(leaf);

            // Where the rows this operator produces end up - genuinely hard to trace by eye through
            // a dozen crossing arrows.
            var path = controller.PathToRoot;
            Assert.IsTrue(path.Contains(leaf));
            Assert.IsTrue(path.Contains(controller.Layout.Root));
            Assert.AreEqual(5, path.Count, "Leaf, hash, sort, gather, and the synthetic root.");

            controller.ClearSelection();
            Assert.AreEqual(0, controller.PathToRoot.Count);
        }

        [TestMethod]
        public void SetHover_OnlyReportsAChangeWhenTheNodeUnderThePointerChanges()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[2];
            var point = controller.ToScreen(node.Bounds.Centre);

            Assert.IsTrue(controller.SetHover(point));
            Assert.IsFalse(controller.SetHover(point), "Still the same node - no repaint needed.");

            Assert.IsNotNull(controller.HoveredTooltip);
            Assert.IsTrue(controller.ClearHover());
            Assert.IsNull(controller.HoveredTooltip);
        }

        [TestMethod]
        public void HoveredTooltip_DescribesTheOperator()
        {
            var controller = Controller();
            var sort = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 1);

            controller.SetHover(controller.ToScreen(sort.Bounds.Centre));
            var tooltip = controller.HoveredTooltip!;

            Assert.AreEqual("Sort", tooltip.Title);

            // The node's own id first - everything that names an operator in words names it by that
            // number - then the measurements, which come before the optimiser's guesses on an actual
            // plan.
            Assert.AreEqual("Node", tooltip.Rows[0].Label);
            Assert.AreEqual("1", tooltip.Rows[0].Value);
            Assert.AreEqual("Actual rows", tooltip.Rows[1].Label);

            var skew = tooltip.Rows.Single(r => r.Label == "Threads");
            StringAssert.Contains(skew.Value, "skew 2.8x");
            Assert.IsTrue(skew.IsEmphasised, "A skew of 2.8 is the answer to why this was slow.");

            Assert.IsTrue(tooltip.Rows.Any(r => r.Label.Contains("spill", StringComparison.OrdinalIgnoreCase)));
            StringAssert.Contains(tooltip.ToString(), "Sort");
        }

        [TestMethod]
        public void HoveredTooltip_OnTheRootDescribesTheWholeStatement()
        {
            var controller = Controller();
            controller.SetHover(controller.ToScreen(controller.Layout.Root.Bounds.Centre));

            var tooltip = controller.HoveredTooltip!;
            Assert.AreEqual("SELECT", tooltip.Title);
            Assert.IsTrue(tooltip.Rows.Any(r => r.Label == "Elapsed time"));
            Assert.IsTrue(tooltip.Rows.Any(r => r.Label == "Memory grant"));
            Assert.IsTrue(tooltip.Rows.Any(r => r.Label == "Grant wait"));

            // Three of eight kilobytes used is worth drawing attention to.
            Assert.IsTrue(tooltip.Rows.Single(r => r.Label == "Memory grant").IsEmphasised);

            // So is a plan compiled for one parameter value and run with another.
            var parameters = tooltip.Rows.Single(r => r.Label == "Parameters");
            Assert.AreEqual("1 of 1 ran with a different value than compiled for", parameters.Value);
            Assert.IsTrue(parameters.IsEmphasised);
        }

        [TestMethod]
        public void HoveredTooltip_OnTheRootSaysNothingAboutParametersThatRanAsCompiled()
        {
            var controller = Controller(TestPlans.KeyLookupSeek);
            controller.SetHover(controller.ToScreen(controller.Layout.Root.Bounds.Centre));

            Assert.IsFalse(controller.HoveredTooltip!.Rows.Any(r => r.Label == "Parameters"));
        }

        [TestMethod]
        public void HeatMetric_IgnoresAMetricThisPlanCannotSupport()
        {
            var controller = Controller(TestPlans.KeyLookupSeek);

            // Carried over from the last plan viewed, an unsupported metric would draw every bar
            // empty rather than falling back.
            controller.HeatMetric = PlanHeatMetric.Cpu;
            Assert.AreEqual(PlanHeatMetric.OperatorCost, controller.HeatMetric);

            var actual = Controller();
            actual.HeatMetric = PlanHeatMetric.Cpu;
            Assert.AreEqual(PlanHeatMetric.Cpu, actual.HeatMetric);
        }

        [TestMethod]
        public void EdgeWidthMetric_RepaintsAndKeepsTheSelection()
        {
            var controller = Controller();
            var sort = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 1);
            controller.Select(sort);

            var raised = 0;
            controller.Changed += (_, _) => raised++;

            controller.EdgeWidthMetric = PlanEdgeWidthMetric.DataSize;

            Assert.AreEqual(PlanEdgeWidthMetric.DataSize, controller.EdgeWidthMetric);
            Assert.AreEqual(1, raised);
            Assert.AreSame(sort, controller.SelectedNode, "Switching measure must not lose the selection.");

            controller.EdgeWidthMetric = PlanEdgeWidthMetric.DataSize;
            Assert.AreEqual(1, raised, "No repaint when nothing changed.");
        }

        [TestMethod]
        public void Constructor_FallsBackWhenTheRequestedMetricIsNotAvailable()
        {
            var controller = Controller(
                TestPlans.KeyLookupSeek,
                new PlanViewOptions { HeatMetric = PlanHeatMetric.Elapsed });

            Assert.AreEqual(PlanHeatMetric.OperatorCost, controller.HeatMetric);
        }

        [TestMethod]
        public void Find_MatchesOperators_Objects_AndPredicates()
        {
            var controller = Controller();

            // The question is nearly always "where does this plan touch that table".
            Assert.IsTrue(controller.Find("Customers") > 0);
            Assert.IsTrue(controller.Matches.All(n =>
                n.Operator!.Objects.Any(o => o.Table == "Customers")));

            Assert.AreEqual(1, controller.Find("Active"), "Matched on the residual predicate.");
            Assert.AreEqual(1, controller.Find("hash match"), "Case insensitive, on the physical op.");
            Assert.AreEqual(0, controller.Find("no such thing"));
        }

        [TestMethod]
        public void Find_TakesANumberAsANodeId()
        {
            var controller = Controller();

            // The cards, the warnings list and the properties panel all name operators by node id,
            // and until now there was no way to turn one back into a node without clicking each.
            Assert.IsTrue(controller.Find("2") > 0);
            Assert.IsTrue(controller.Matches.Any(n => n.Operator?.NodeId == 2));

            Assert.AreEqual(0, controller.Find("9999"), "No node, and no text with those digits in it.");
        }

        [TestMethod]
        public void NextMatch_StepsThroughTheMatchesAndWrapsRound()
        {
            var controller = Controller();
            var count = controller.Find("Scan");
            Assert.IsTrue(count >= 2, "Need more than one match to step through.");

            Assert.IsTrue(controller.NextMatch());
            Assert.AreEqual(0, controller.MatchIndex);
            Assert.AreSame(controller.Matches[0], controller.SelectedNode);

            for (var i = 1; i < count; i++) controller.NextMatch();
            Assert.AreEqual(count - 1, controller.MatchIndex);

            controller.NextMatch();
            Assert.AreEqual(0, controller.MatchIndex, "Wraps rather than stopping at the end.");

            controller.NextMatch(-1);
            Assert.AreEqual(count - 1, controller.MatchIndex, "And wraps backwards too.");
        }

        [TestMethod]
        public void NextMatch_DoesNothingWithNoMatches()
        {
            var controller = Controller();
            controller.Find("no such thing");

            Assert.IsFalse(controller.NextMatch());
        }

        [TestMethod]
        public void MoveSelection_WalksTheTreeInTheDirectionItIsDrawn()
        {
            var controller = Controller();

            // Nothing selected, so any direction starts at the head of the plan.
            Assert.IsTrue(controller.MoveSelection(PlanNavigation.Input));
            Assert.AreSame(controller.Layout.Root, controller.SelectedNode);

            // Rightwards on screen is into the inputs.
            controller.MoveSelection(PlanNavigation.Input);
            Assert.AreEqual(0, controller.SelectedNode!.Operator!.NodeId);

            // ...and leftwards is back towards the root.
            controller.MoveSelection(PlanNavigation.Consumer);
            Assert.AreSame(controller.Layout.Root, controller.SelectedNode);

            Assert.IsFalse(controller.MoveSelection(PlanNavigation.Consumer), "The root has no consumer.");
        }

        [TestMethod]
        public void MoveSelection_StepsWithinTheColumnRatherThanBetweenSiblings()
        {
            var controller = Controller();

            var column = controller.Layout.Nodes
                .Where(n => n.Depth == 4)
                .OrderBy(n => n.Bounds.Top)
                .ToList();

            Assert.AreEqual(2, column.Count);

            controller.Select(column[0]);
            Assert.IsTrue(controller.MoveSelection(PlanNavigation.Down));

            // Down means "the box below this one", which is what the picture shows.
            Assert.AreSame(column[1], controller.SelectedNode);

            Assert.IsFalse(controller.MoveSelection(PlanNavigation.Down), "Nothing below the last one.");
            Assert.IsTrue(controller.MoveSelection(PlanNavigation.Up));
            Assert.AreSame(column[0], controller.SelectedNode);
        }

        [TestMethod]
        public void ToggleCollapse_KeepsTheSelectionOnTheOperatorItWasOn()
        {
            var controller = Controller();
            var sort = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 1);
            controller.Select(sort);

            var hash = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 2);
            Assert.IsTrue(controller.ToggleCollapse(hash));

            // Every node object was replaced by the rebuild, so the selection is re-established by
            // operator rather than by reference.
            Assert.AreEqual(1, controller.SelectedNode!.Operator!.NodeId);
            Assert.IsFalse(ReferenceEquals(sort, controller.SelectedNode));
        }

        [TestMethod]
        public void ToggleCollapse_MovesASelectionThatIsNowHiddenOntoTheCollapsedNode()
        {
            var controller = Controller();
            var leaf = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 4);
            controller.Select(leaf);

            var hash = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 2);
            controller.ToggleCollapse(hash);

            // The selected operator is under the collapsed one now, and that is where the selection
            // honestly belongs.
            Assert.AreEqual(2, controller.SelectedNode!.Operator!.NodeId);
            Assert.IsNull(controller.HoveredNode, "Hover pointed at a node that no longer exists.");
        }

        [TestMethod]
        public void EdgeWidthBasis_ReroutesTheArrowsAndKeepsTheSelection()
        {
            var controller = Controller();
            var sort = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 1);
            controller.Select(sort);
            var changed = false;
            controller.Changed += (_, _) => changed = true;

            controller.EdgeWidthBasis = PlanEdgeWidthBasis.Estimated;

            Assert.IsTrue(changed, "The arrows changed, so the host has to repaint.");
            Assert.AreEqual(PlanEdgeWidthBasis.Estimated, controller.Layout.EffectiveEdgeWidthBasis);
            Assert.AreSame(sort, controller.SelectedNode, "Only the arrows were routed again.");
        }

        [TestMethod]
        public void ToggleCollapse_WithNothingSelectedSelectsNothing()
        {
            var controller = Controller();

            controller.ToggleCollapse(controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 2));

            // Selecting the root here would open the properties panel on a click that only collapsed
            // something.
            Assert.IsNull(controller.SelectedNode);
        }

        [TestMethod]
        public void ExpandAll_CarriesTheSelectionOverToTheNewNodes()
        {
            var controller = Controller();
            var hash = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 2);
            controller.ToggleCollapse(hash);

            var sort = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 1);
            controller.Select(sort);

            var changed = false;
            controller.Changed += (_, _) => changed = true;

            Assert.IsTrue(controller.ExpandAll());
            Assert.IsFalse(controller.Layout.HasCollapsedNodes);
            Assert.IsTrue(changed, "The plan was laid out again, so the host has to repaint.");

            // Expanding replaces every node object, like collapsing does.  Left pointing at the old
            // ones, nothing draws as selected and the path highlight matches no node at all.
            Assert.AreEqual(1, controller.SelectedNode!.Operator!.NodeId);
            Assert.IsTrue(controller.Layout.Nodes.Contains(controller.SelectedNode));
            Assert.IsTrue(controller.PathToRoot.All(n => controller.Layout.Nodes.Contains(n)));
        }

        [TestMethod]
        public void ExpandAll_RerunsTheSearchAndForgetsTheHover()
        {
            var controller = Controller();
            controller.ToggleCollapse(controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 2));

            controller.Find("Orders");
            Assert.AreEqual(0, controller.Matches.Count, "The matching node is hidden while collapsed.");

            var collapsed = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 2);
            controller.SetHover(controller.ToScreen(collapsed.Bounds.Centre));
            Assert.IsNotNull(controller.HoveredNode);

            controller.ExpandAll();

            // The match is back, against a node that is actually drawn, and the hover no longer
            // describes a node that has been replaced.
            Assert.AreEqual(1, controller.Matches.Count);
            Assert.IsTrue(controller.Layout.Nodes.Contains(controller.Matches[0]));
            Assert.IsNull(controller.HoveredNode);
            Assert.IsNull(controller.HoveredTooltip);
        }

        [TestMethod]
        public void ExpandAll_DoesNothingWithNothingCollapsed()
        {
            var controller = Controller();
            var sort = controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 1);
            controller.Select(sort);

            Assert.IsFalse(controller.ExpandAll());
            Assert.AreSame(sort, controller.SelectedNode, "No rebuild, so the node objects stand.");
        }

        [TestMethod]
        public void ToggleCollapse_RerunsTheSearchAgainstTheNewNodes()
        {
            var controller = Controller();
            controller.Find("Orders");
            Assert.AreEqual(1, controller.Matches.Count);

            controller.ToggleCollapse(controller.Layout.Nodes.Single(n => n.Operator?.NodeId == 2));

            // The matching node is hidden now, so it must not be left in the list pointing at a node
            // that is no longer drawn.
            Assert.AreEqual(0, controller.Matches.Count);
        }

        [TestMethod]
        public void SelectOperator_BringsAnOffScreenNodeIntoView()
        {
            var controller = Controller(width: 200, height: 200);
            controller.SetZoom(4, new LayoutPoint(0, 0));

            var target = TestPlans.Operator(controller.Layout.Statement, 4);
            Assert.IsTrue(controller.SelectOperator(target));

            Assert.AreEqual(4, controller.SelectedNode!.Operator!.NodeId);
            Assert.IsTrue(
                controller.VisibleBounds.IntersectsWith(controller.SelectedNode.Bounds),
                "A selection made from elsewhere in the viewer has to be visible.");
        }

        [TestMethod]
        public void EnsureVisible_LeavesTheViewAloneForANodeAlreadyOnScreen()
        {
            var controller = Controller();
            var node = controller.Layout.Root;
            var pan = controller.Pan;

            Assert.IsFalse(controller.EnsureVisible(node));
            Assert.AreEqual(pan, controller.Pan, "Jerking the view for no reason is worse than not moving.");
        }

        [TestMethod]
        public void Activate_RaisesForTheHostToDrillInto()
        {
            var controller = Controller();
            PlanNode? activated = null;
            controller.NodeActivated += (_, node) => activated = node;

            controller.Activate(controller.Layout.Nodes[1]);
            Assert.AreSame(controller.Layout.Nodes[1], activated);

            activated = null;
            controller.Activate(null);
            Assert.IsNull(activated);
        }

        [TestMethod]
        public void Changed_FiresForAnythingThatAltersWhatIsDrawn()
        {
            var controller = Controller();
            var raised = 0;
            controller.Changed += (_, _) => raised++;

            controller.ZoomIn(new LayoutPoint(10, 10));
            controller.PanBy(5, 5);
            controller.Select(controller.Layout.Nodes[1]);
            controller.HeatMetric = PlanHeatMetric.Cpu;

            Assert.AreEqual(4, raised);
        }

        [TestMethod]
        public void Constructor_RejectsANullLayout()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new PlanViewController(null!));
        }
    }
}
