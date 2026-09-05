using System;
using System.Linq;
using DBADash.Deadlock.Interaction;
using DBADash.Deadlock.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// View state: transforms, zoom, pan, selection and hover.  None of this needs a window, which
    /// is the point of keeping it out of the control.
    /// </summary>
    [TestClass]
    public class DeadlockViewControllerTests
    {
        private const double Epsilon = 1e-9;

        private static DeadlockLayout SampleLayout(string sampleName = TestGraphs.KeyLock)
        {
            var graph = DeadlockParser.Parse(TestGraphs.Load(sampleName)).First();
            return new DeadlockLayoutEngine(new FakeTextMeasurer()).Layout(graph);
        }

        private static DeadlockViewController Controller(
            DeadlockViewOptions? options = null,
            string sampleName = TestGraphs.KeyLock) =>
            new(SampleLayout(sampleName), options);

        // ---------------------------------------------------------------- transforms

        [TestMethod]
        public void ToScreen_AppliesZoomThenPan()
        {
            var controller = Controller();
            controller.SetZoom(2, new LayoutPoint(0, 0));
            controller.PanBy(30, 40);

            var screen = controller.ToScreen(new LayoutPoint(10, 5));

            Assert.AreEqual(50, screen.X, Epsilon);
            Assert.AreEqual(50, screen.Y, Epsilon);
        }

        [TestMethod]
        public void ToLayout_IsTheInverseOfToScreen()
        {
            var controller = Controller();
            controller.SetZoom(1.7, new LayoutPoint(120, 90));
            controller.PanBy(-35, 22);

            var original = new LayoutPoint(43.5, 172.25);
            var roundTripped = controller.ToLayout(controller.ToScreen(original));

            Assert.AreEqual(original.X, roundTripped.X, 1e-6);
            Assert.AreEqual(original.Y, roundTripped.Y, 1e-6);
        }

        [TestMethod]
        public void ToScreen_ScalesRectanglePositionAndSize()
        {
            var controller = Controller();
            controller.SetZoom(3, new LayoutPoint(0, 0));
            controller.PanBy(10, 20);

            var screen = controller.ToScreen(new LayoutRect(5, 5, 40, 20));

            Assert.AreEqual(25, screen.X, Epsilon);
            Assert.AreEqual(35, screen.Y, Epsilon);
            Assert.AreEqual(120, screen.Width, Epsilon);
            Assert.AreEqual(60, screen.Height, Epsilon);
        }

        // ---------------------------------------------------------------- zoom

        [TestMethod]
        public void SetZoom_KeepsThePointUnderTheAnchorStable()
        {
            // This is what makes wheel zoom feel right - whatever is under the pointer must not drift.
            var controller = Controller();
            controller.PanBy(17, -23);

            var anchor = new LayoutPoint(140, 95);
            var before = controller.ToLayout(anchor);

            controller.SetZoom(3.5, anchor);
            var after = controller.ToLayout(anchor);

            Assert.AreEqual(before.X, after.X, 1e-6);
            Assert.AreEqual(before.Y, after.Y, 1e-6);
        }

        [TestMethod]
        public void SetZoom_ClampsToTheConfiguredRange()
        {
            var controller = Controller(new DeadlockViewOptions { MinZoom = 0.5, MaxZoom = 4 });
            var anchor = new LayoutPoint(0, 0);

            controller.SetZoom(100, anchor);
            Assert.AreEqual(4, controller.Zoom, Epsilon);

            controller.SetZoom(0.001, anchor);
            Assert.AreEqual(0.5, controller.Zoom, Epsilon);
        }

        [TestMethod]
        public void ZoomIn_AndZoomOut_ApplyTheConfiguredStep()
        {
            var controller = Controller(new DeadlockViewOptions { ZoomStep = 2 });
            var anchor = new LayoutPoint(50, 50);

            controller.ZoomIn(anchor);
            Assert.AreEqual(2, controller.Zoom, Epsilon);

            controller.ZoomOut(anchor);
            Assert.AreEqual(1, controller.Zoom, Epsilon);
        }

        [TestMethod]
        public void ZoomIn_ThenZoomOut_AboutTheSameAnchor_RestoresTheView()
        {
            var controller = Controller();
            controller.PanBy(12, 34);
            var anchor = new LayoutPoint(200, 150);
            var panBefore = controller.Pan;

            controller.ZoomIn(anchor);
            controller.ZoomOut(anchor);

            Assert.AreEqual(1, controller.Zoom, 1e-9);
            Assert.AreEqual(panBefore.X, controller.Pan.X, 1e-6);
            Assert.AreEqual(panBefore.Y, controller.Pan.Y, 1e-6);
        }

        // ---------------------------------------------------------------- fit

        [TestMethod]
        public void ZoomToFit_ScalesToTheTighterAxis()
        {
            var controller = Controller();
            var bounds = controller.Layout.Bounds;
            controller.SetViewport(new LayoutSize(bounds.Width * 4, bounds.Height / 2));

            controller.ZoomToFit();

            Assert.AreEqual(0.5, controller.Zoom, 1e-9);
        }

        [TestMethod]
        public void ZoomToFit_CentresTheGraphInTheViewport()
        {
            var controller = Controller();
            var bounds = controller.Layout.Bounds;
            controller.SetViewport(new LayoutSize(bounds.Width * 2, bounds.Height * 2));

            controller.ZoomToFit();

            // Not enlarged, so the graph keeps its size and the spare room is split evenly.
            Assert.AreEqual(bounds.Width / 2, controller.Pan.X, 1e-6);
            Assert.AreEqual(bounds.Height / 2, controller.Pan.Y, 1e-6);
        }

        [TestMethod]
        public void ZoomToFit_DoesNotEnlargeBeyondFullSizeByDefault()
        {
            var controller = Controller();
            var bounds = controller.Layout.Bounds;
            controller.SetViewport(new LayoutSize(bounds.Width * 5, bounds.Height * 5));

            controller.ZoomToFit();

            Assert.AreEqual(1, controller.Zoom, Epsilon);
        }

        [TestMethod]
        public void ZoomToFit_EnlargesUpToTheFitZoomCap()
        {
            var controller = Controller(new DeadlockViewOptions { MaxFitZoom = 3 });
            var bounds = controller.Layout.Bounds;
            controller.SetViewport(new LayoutSize(bounds.Width * 3, bounds.Height * 3));

            controller.ZoomToFit();

            Assert.AreEqual(3, controller.Zoom, 1e-9);
        }

        [TestMethod]
        public void ZoomToFit_PutsEveryNodeInsideTheViewport()
        {
            var controller = Controller(sampleName: TestGraphs.ThreeWay);
            var viewport = new LayoutSize(500, 320);
            controller.SetViewport(viewport);

            controller.ZoomToFit();

            foreach (var node in controller.Layout.Nodes)
            {
                var screen = controller.ToScreen(node.Bounds);
                Assert.IsTrue(screen.Left >= -1e-6, $"{node.Title} is off the left edge.");
                Assert.IsTrue(screen.Top >= -1e-6, $"{node.Title} is off the top edge.");
                Assert.IsTrue(screen.Right <= viewport.Width + 1e-6, $"{node.Title} is off the right edge.");
                Assert.IsTrue(screen.Bottom <= viewport.Height + 1e-6, $"{node.Title} is off the bottom edge.");
            }
        }

        [TestMethod]
        public void ZoomToFit_DoesNothingWithoutAViewport()
        {
            var controller = Controller();

            Assert.IsFalse(controller.ZoomToFit());
            Assert.AreEqual(1, controller.Zoom, Epsilon);
        }

        [TestMethod]
        public void ZoomToFit_DoesNothingForAnEmptyLayout()
        {
            var graph = DeadlockParser.Parse("<deadlock><process-list /></deadlock>").First();
            var controller = new DeadlockViewController(
                new DeadlockLayoutEngine(new FakeTextMeasurer()).Layout(graph));
            controller.SetViewport(new LayoutSize(400, 300));

            Assert.IsFalse(controller.ZoomToFit());
        }

        [TestMethod]
        public void ResetView_ReturnsToFullSizeAtTheOrigin()
        {
            var controller = Controller();
            controller.SetZoom(2.5, new LayoutPoint(30, 30));
            controller.PanBy(80, 90);

            controller.ResetView();

            Assert.AreEqual(1, controller.Zoom, Epsilon);
            Assert.AreEqual(new LayoutPoint(0, 0), controller.Pan);
        }

        // ---------------------------------------------------------------- pan

        [TestMethod]
        public void PanBy_ShiftsTheContent()
        {
            var controller = Controller();

            Assert.IsTrue(controller.PanBy(15, -25));
            Assert.AreEqual(new LayoutPoint(15, -25), controller.Pan);
        }

        [TestMethod]
        public void PanBy_NoMovement_ReportsNoChange()
        {
            var controller = Controller();

            Assert.IsFalse(controller.PanBy(0, 0));
        }

        [TestMethod]
        public void PanTo_WithoutBeginPan_DoesNothing()
        {
            var controller = Controller();

            Assert.IsFalse(controller.PanTo(new LayoutPoint(50, 50)));
            Assert.AreEqual(new LayoutPoint(0, 0), controller.Pan);
        }

        [TestMethod]
        public void PanTo_TracksThePointerExactlyOverASequenceOfMoves()
        {
            // The anchor advances with each move, so a long drag must not accelerate away from the
            // pointer.
            var controller = Controller();
            var grabbedAt = new LayoutPoint(100, 100);
            var contentUnderPointer = controller.ToLayout(grabbedAt);

            controller.BeginPan(grabbedAt);
            controller.PanTo(new LayoutPoint(120, 130));
            controller.PanTo(new LayoutPoint(90, 200));
            controller.PanTo(new LayoutPoint(310, 45));

            var nowAt = controller.ToScreen(contentUnderPointer);
            Assert.AreEqual(310, nowAt.X, 1e-6);
            Assert.AreEqual(45, nowAt.Y, 1e-6);
        }

        [TestMethod]
        public void EndPan_StopsTheGesture()
        {
            var controller = Controller();
            controller.BeginPan(new LayoutPoint(10, 10));
            Assert.IsTrue(controller.IsPanning);

            controller.EndPan();

            Assert.IsFalse(controller.IsPanning);
            Assert.IsFalse(controller.PanTo(new LayoutPoint(200, 200)));
        }

        // ---------------------------------------------------------------- moving nodes

        [TestMethod]
        public void BeginNodeDrag_OnEmptySpace_DoesNotStart()
        {
            var controller = Controller();

            // Far outside the graph, so there is nothing to pick up - the host pans instead.
            Assert.IsFalse(controller.BeginNodeDrag(new LayoutPoint(-500, -500)));
            Assert.IsFalse(controller.IsDraggingNode);
            Assert.IsFalse(controller.DragNodeTo(new LayoutPoint(0, 0)));
        }

        [TestMethod]
        public void DragNodeTo_MovesOnlyTheDraggedNode()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[0];
            var others = controller.Layout.Nodes.Skip(1).ToDictionary(n => n, n => n.Bounds);
            var before = node.Bounds;

            Assert.IsTrue(controller.BeginNodeDrag(controller.ToScreen(node.Bounds.Centre)));
            controller.DragNodeTo(controller.ToScreen(node.Bounds.Centre.Offset(60, -25)));

            Assert.AreEqual(before.X + 60, node.Bounds.X, 1e-6);
            Assert.AreEqual(before.Y - 25, node.Bounds.Y, 1e-6);
            Assert.AreEqual(before.Width, node.Bounds.Width, 1e-6);

            foreach (var (other, bounds) in others)
            {
                Assert.AreEqual(bounds, other.Bounds, $"{other.Title} moved.");
            }
        }

        /// <summary>
        /// The anchor follows the pointer, so a long drag lands exactly where the pointer is rather
        /// than accelerating away from it - the same contract <see cref="DeadlockViewController.PanTo"/>
        /// keeps.
        /// </summary>
        [TestMethod]
        public void DragNodeTo_TracksThePointerExactlyOverASequenceOfMoves()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[0];
            var grabbedAt = controller.ToScreen(node.Bounds.Centre.Offset(5, 5));
            var before = node.Bounds;

            controller.BeginNodeDrag(grabbedAt);
            controller.DragNodeTo(grabbedAt.Offset(40, 10));
            controller.DragNodeTo(grabbedAt.Offset(-15, 90));
            controller.DragNodeTo(grabbedAt.Offset(200, -60));

            Assert.AreEqual(before.X + 200, node.Bounds.X, 1e-6);
            Assert.AreEqual(before.Y - 60, node.Bounds.Y, 1e-6);
        }

        /// <summary>
        /// The whole point of moving a node: the arrows have to come with it, still clipped to the
        /// borders of the boxes they join.
        /// </summary>
        [TestMethod]
        public void DragNodeTo_ReRoutesTheEdgesThatMeetTheNode()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[0];
            var edge = controller.Layout.Edges.First(e => ReferenceEquals(e.From, node));
            var before = edge.Start;

            controller.BeginNodeDrag(controller.ToScreen(node.Bounds.Centre));
            controller.DragNodeTo(controller.ToScreen(node.Bounds.Centre.Offset(150, 150)));

            // The edge list is rebuilt, so find the same edge again rather than reusing the instance.
            var routed = controller.Layout.Edges.Single(e =>
                ReferenceEquals(e.From, edge.From) && ReferenceEquals(e.To, edge.To) && e.Kind == edge.Kind);

            Assert.AreNotEqual(before, routed.Start);
            Assert.IsTrue(OnBorderOf(node.Bounds, routed.Start),
                $"The arrow starts at {routed.Start}, which is not on the moved node's border {node.Bounds}.");
        }

        /// <summary>
        /// Dragging a node off the top left takes the layout's origin negative.  Nothing is shifted to
        /// compensate - that would move the whole graph out from under the reader - so the bounds have
        /// to carry the new origin, and fitting has to honour it.
        /// </summary>
        [TestMethod]
        public void DragNodeTo_GrowsTheBoundsRatherThanShiftingEverything()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[0];

            // By reference, not by index: picking a node up brings it to the front of the list.
            var neighbour = controller.Layout.Nodes[1];
            var untouched = neighbour.Bounds;

            controller.BeginNodeDrag(controller.ToScreen(node.Bounds.Centre));
            controller.DragNodeTo(controller.ToScreen(node.Bounds.Centre.Offset(-400, -300)));

            Assert.IsTrue(controller.Layout.Bounds.X < 0, "The bounds should have followed the node.");
            Assert.IsTrue(controller.Layout.Bounds.Contains(node.Bounds.Centre));
            Assert.AreEqual(untouched, neighbour.Bounds, "Everything else should have stayed put.");
        }

        [TestMethod]
        public void ZoomToFit_AfterANodeIsDraggedOutwards_StillShowsEveryNode()
        {
            var controller = Controller(sampleName: TestGraphs.ThreeWay);
            var viewport = new LayoutSize(500, 320);
            controller.SetViewport(viewport);

            var node = controller.Layout.Nodes[0];
            controller.BeginNodeDrag(controller.ToScreen(node.Bounds.Centre));
            controller.DragNodeTo(controller.ToScreen(node.Bounds.Centre.Offset(-600, -450)));
            controller.EndNodeDrag();

            controller.ZoomToFit();

            foreach (var laidOut in controller.Layout.Nodes)
            {
                var screen = controller.ToScreen(laidOut.Bounds);
                Assert.IsTrue(screen.Left >= -1e-6, $"{laidOut.Title} is off the left edge.");
                Assert.IsTrue(screen.Top >= -1e-6, $"{laidOut.Title} is off the top edge.");
                Assert.IsTrue(screen.Right <= viewport.Width + 1e-6, $"{laidOut.Title} is off the right edge.");
                Assert.IsTrue(screen.Bottom <= viewport.Height + 1e-6, $"{laidOut.Title} is off the bottom edge.");
            }
        }

        /// <summary>
        /// A press that never moved is a click, and the host uses that to tell a drag from a click on
        /// a statement link - dragging a box by its statement must not also open it.
        /// </summary>
        [TestMethod]
        public void EndNodeDrag_ReportsWhetherTheNodeActuallyMoved()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[0];
            var centre = controller.ToScreen(node.Bounds.Centre);

            controller.BeginNodeDrag(centre);
            Assert.IsFalse(controller.EndNodeDrag(), "A press that did not move is a click.");

            controller.BeginNodeDrag(centre);
            controller.DragNodeTo(centre.Offset(30, 0));
            Assert.IsTrue(controller.EndNodeDrag());

            Assert.IsFalse(controller.IsDraggingNode);
            Assert.IsFalse(controller.DragNodeTo(centre.Offset(90, 0)), "The gesture is over.");
        }

        /// <summary>Hit testing follows a moved node, or it could not be picked up again.</summary>
        [TestMethod]
        public void HitTest_FindsANodeAtItsNewPosition()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[0];
            var from = controller.ToScreen(node.Bounds.Centre);

            controller.BeginNodeDrag(from);
            controller.DragNodeTo(from.Offset(220, 140));
            controller.EndNodeDrag();

            Assert.AreSame(node, controller.HitTest(controller.ToScreen(node.Bounds.Centre)));
        }

        /// <summary>A point on the border of a rectangle, within a small tolerance.</summary>
        private static bool OnBorderOf(LayoutRect rect, LayoutPoint point)
        {
            const double tolerance = 1e-6;

            var insideX = point.X >= rect.Left - tolerance && point.X <= rect.Right + tolerance;
            var insideY = point.Y >= rect.Top - tolerance && point.Y <= rect.Bottom + tolerance;

            var onVerticalEdge = Math.Abs(point.X - rect.Left) < tolerance || Math.Abs(point.X - rect.Right) < tolerance;
            var onHorizontalEdge = Math.Abs(point.Y - rect.Top) < tolerance || Math.Abs(point.Y - rect.Bottom) < tolerance;

            return (onVerticalEdge && insideY) || (onHorizontalEdge && insideX);
        }

        // ---------------------------------------------------------------- picking

        [TestMethod]
        public void HitTest_AccountsForZoomAndPan()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[1];
            controller.SetZoom(2, new LayoutPoint(0, 0));
            controller.PanBy(60, 45);

            Assert.AreSame(node, controller.HitTest(controller.ToScreen(node.Bounds.Centre)));
        }

        [TestMethod]
        public void SelectAt_SelectsTheNodeUnderThePoint()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[2];

            Assert.IsTrue(controller.SelectAt(controller.ToScreen(node.Bounds.Centre)));
            Assert.AreSame(node, controller.SelectedNode);
        }

        [TestMethod]
        public void SelectAt_EmptySpace_ClearsTheSelection()
        {
            var controller = Controller();
            controller.Select(controller.Layout.Nodes[0]);

            // Inside the margin, which is empty by construction.
            Assert.IsTrue(controller.SelectAt(new LayoutPoint(1, 1)));
            Assert.IsNull(controller.SelectedNode);
        }

        [TestMethod]
        public void Select_TheSameNodeAgain_ReportsNoChange()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[0];

            Assert.IsTrue(controller.Select(node));
            Assert.IsFalse(controller.Select(node));
        }

        [TestMethod]
        public void SetHover_ReportsChangeOnlyWhenTheHoveredNodeChanges()
        {
            var controller = Controller();
            var node = controller.Layout.Nodes[0];
            var centre = controller.ToScreen(node.Bounds.Centre);

            Assert.IsTrue(controller.SetHover(centre), "Entering a node is a change.");
            Assert.IsFalse(
                controller.SetHover(centre.Offset(1, 1)),
                "Moving within the same node is not a change, so the host should not repaint.");
            Assert.IsTrue(controller.SetHover(new LayoutPoint(1, 1)), "Leaving a node is a change.");
        }

        [TestMethod]
        public void SetHover_BuildsTheTooltipForTheHoveredNode()
        {
            var controller = Controller();
            var victim = controller.Layout.Nodes[0];

            controller.SetHover(controller.ToScreen(victim.Bounds.Centre));

            Assert.AreSame(victim, controller.HoveredNode);
            Assert.IsNotNull(controller.HoveredTooltip);
            Assert.AreEqual(victim.Title, controller.HoveredTooltip!.Title);
        }

        [TestMethod]
        public void ClearHover_DropsTheNodeAndItsTooltip()
        {
            var controller = Controller();
            controller.SetHover(controller.ToScreen(controller.Layout.Nodes[0].Bounds.Centre));

            Assert.IsTrue(controller.ClearHover());
            Assert.IsNull(controller.HoveredNode);
            Assert.IsNull(controller.HoveredTooltip);
            Assert.IsFalse(controller.ClearHover(), "Already cleared.");
        }

        // ---------------------------------------------------------------- notification

        [TestMethod]
        public void Changed_IsRaisedWhenTheViewMoves()
        {
            var controller = Controller();
            var raised = 0;
            controller.Changed += (_, _) => raised++;

            controller.PanBy(10, 10);
            controller.SetZoom(2, new LayoutPoint(0, 0));
            controller.Select(controller.Layout.Nodes[0]);

            Assert.AreEqual(3, raised);
        }

        [TestMethod]
        public void Changed_IsNotRaisedWhenNothingActuallyChanges()
        {
            var controller = Controller();
            controller.SetViewport(new LayoutSize(400, 300));
            var raised = 0;
            controller.Changed += (_, _) => raised++;

            controller.PanBy(0, 0);
            controller.SetViewport(new LayoutSize(400, 300));
            controller.ClearSelection();
            controller.ClearHover();

            Assert.AreEqual(0, raised);
        }

        // ---------------------------------------------------------------- construction

        [TestMethod]
        public void Constructor_NullLayout_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DeadlockViewController(null!));
        }

        [TestMethod]
        public void SetViewport_TheSameSizeAgain_ReportsNoChange()
        {
            var controller = Controller();

            Assert.IsTrue(controller.SetViewport(new LayoutSize(800, 600)));
            Assert.IsFalse(controller.SetViewport(new LayoutSize(800, 600)));
        }

        // ---------------------------------------------------------------- refit on resize

        [TestMethod]
        public void SetViewport_RefitsWhileTheViewHasNotBeenTouched()
        {
            var controller = Controller();
            var bounds = controller.Layout.Bounds;

            controller.SetViewport(new LayoutSize(bounds.Width, bounds.Height));
            Assert.AreEqual(1, controller.Zoom, 1e-9);

            // Halving the window should shrink the graph to keep all of it visible.
            controller.SetViewport(new LayoutSize(bounds.Width / 2, bounds.Height / 2));

            Assert.AreEqual(0.5, controller.Zoom, 1e-9);
        }

        [TestMethod]
        public void SetViewport_KeepsTheZoomTheUserChose()
        {
            var controller = Controller();
            var bounds = controller.Layout.Bounds;
            controller.SetViewport(new LayoutSize(bounds.Width, bounds.Height));

            controller.SetZoom(2, new LayoutPoint(10, 10));
            controller.SetViewport(new LayoutSize(bounds.Width / 2, bounds.Height / 2));

            Assert.AreEqual(2, controller.Zoom, 1e-9);
        }

        [TestMethod]
        public void SetViewport_KeepsThePanTheUserChose()
        {
            var controller = Controller();
            var bounds = controller.Layout.Bounds;
            controller.SetViewport(new LayoutSize(bounds.Width, bounds.Height));

            controller.PanBy(25, -15);
            var panned = controller.Pan;
            controller.SetViewport(new LayoutSize(bounds.Width * 2, bounds.Height * 2));

            Assert.AreEqual(panned, controller.Pan);
        }

        [TestMethod]
        public void ZoomToFit_ResumesFollowingTheWindow()
        {
            // Fit is also how the user says "start tracking the window again".
            var controller = Controller();
            var bounds = controller.Layout.Bounds;
            controller.SetViewport(new LayoutSize(bounds.Width, bounds.Height));
            controller.SetZoom(3, new LayoutPoint(0, 0));

            Assert.IsTrue(controller.IsViewUserAdjusted);
            controller.ZoomToFit();
            Assert.IsFalse(controller.IsViewUserAdjusted);

            controller.SetViewport(new LayoutSize(bounds.Width / 2, bounds.Height / 2));

            Assert.AreEqual(0.5, controller.Zoom, 1e-9);
        }

        [TestMethod]
        public void ResetView_CountsAsTheUserTakingControl()
        {
            var controller = Controller();
            var bounds = controller.Layout.Bounds;
            controller.SetViewport(new LayoutSize(bounds.Width, bounds.Height));

            controller.ResetView();
            controller.SetViewport(new LayoutSize(bounds.Width / 4, bounds.Height / 4));

            Assert.IsTrue(controller.IsViewUserAdjusted);
            Assert.AreEqual(1, controller.Zoom, 1e-9);
        }

        [TestMethod]
        public void SetViewport_DoesNotRefitWhenTheOptionIsOff()
        {
            var controller = Controller(new DeadlockViewOptions { RefitOnViewportChange = false });
            var bounds = controller.Layout.Bounds;

            controller.SetViewport(new LayoutSize(bounds.Width, bounds.Height));
            controller.SetViewport(new LayoutSize(bounds.Width / 2, bounds.Height / 2));

            Assert.AreEqual(1, controller.Zoom, 1e-9);
        }

        [TestMethod]
        public void SelectionAndHover_DoNotCountAsAdjustingTheView()
        {
            var controller = Controller();
            controller.SetViewport(new LayoutSize(900, 700));

            controller.Select(controller.Layout.Nodes[0]);
            controller.SetHover(controller.ToScreen(controller.Layout.Nodes[0].Bounds.Centre));

            Assert.IsFalse(
                controller.IsViewUserAdjusted,
                "Picking a node says nothing about where the user wants the view.");
        }

        // ---------------------------------------------------------------- statement activation

        private static DeadlockProcessNode NodeWithAStatement(DeadlockViewController controller) =>
            controller.Layout.Nodes
                .OfType<DeadlockProcessNode>()
                .First(n => !string.IsNullOrWhiteSpace(n.Process.PrimaryStatement));

        [TestMethod]
        public void ActivateStatement_RaisesTheFullStatementForTheNode()
        {
            var controller = Controller();
            var node = NodeWithAStatement(controller);

            DeadlockStatementActivatedEventArgs? raised = null;
            controller.StatementActivated += (_, e) => raised = e;

            Assert.IsTrue(controller.ActivateStatement(node));
            Assert.AreSame(node, raised!.Node);
            Assert.AreEqual(node.Process.PrimaryStatement, raised.Statement);
        }

        [TestMethod]
        public void ActivateStatement_DoesNothingForANodeWithNoStatement()
        {
            var controller = Controller();
            var resource = controller.Layout.Nodes.OfType<DeadlockResourceNode>().First();

            var raised = false;
            controller.StatementActivated += (_, _) => raised = true;

            Assert.IsFalse(controller.ActivateStatement(resource));
            Assert.IsFalse(raised, "A resource has no statement to open.");
        }

        [TestMethod]
        public void ActivateStatement_DoesNothingWhenNobodyIsListening()
        {
            var controller = Controller();

            Assert.IsFalse(
                controller.ActivateStatement(NodeWithAStatement(controller)),
                "Nothing can open the statement, so the caller needs to know not to offer it.");
        }
    }
}
