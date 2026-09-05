using System;
using System.Linq;
using DBADash.Deadlock.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// Geometry and ordering produced by the layout engine.  No drawing is involved: the engine is
    /// exercised through a fake text measurer, which is the seam that keeps it renderer agnostic.
    /// </summary>
    [TestClass]
    public class DeadlockLayoutEngineTests
    {
        private const double Epsilon = 1e-6;

        private static DeadlockLayout LayoutSample(string sampleName, DeadlockLayoutOptions? options = null) =>
            LayoutXml(TestGraphs.Load(sampleName), options);

        private static DeadlockLayout LayoutXml(string xml, DeadlockLayoutOptions? options = null)
        {
            var graph = DeadlockParser.Parse(xml).First();
            return new DeadlockLayoutEngine(new FakeTextMeasurer(), options).Layout(graph);
        }

        /// <summary>
        /// The clear space between two rectangles.  Axis aligned boxes are apart when they are apart
        /// on either axis, so the separation is the larger of the two gaps; negative means they
        /// overlap on both.
        /// </summary>
        private static double GapBetween(LayoutRect a, LayoutRect b) =>
            Math.Max(
                Math.Max(b.Left - a.Right, a.Left - b.Right),
                Math.Max(b.Top - a.Bottom, a.Top - b.Bottom));

        private static double Distance(LayoutPoint a, LayoutPoint b) =>
            Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

        /// <summary>
        /// The perpendicular gap between two parallel edges, measured from one line's start to the
        /// other line.  The two arrows between a shared pair run in opposite directions, so comparing
        /// endpoints directly would measure the length of the line rather than the gap.
        /// </summary>
        private static double DistanceBetweenLines(DeadlockEdge a, DeadlockEdge b)
        {
            var dx = a.End.X - a.Start.X;
            var dy = a.End.Y - a.Start.Y;
            var length = Math.Sqrt((dx * dx) + (dy * dy));

            // |cross product| / |a| is the distance from b's start to the infinite line through a.
            var cross = ((b.Start.X - a.Start.X) * dy) - ((b.Start.Y - a.Start.Y) * dx);
            return Math.Abs(cross) / length;
        }

        private static bool Overlaps(LayoutRect a, LayoutRect b) =>
            a.Left < b.Right - Epsilon &&
            b.Left < a.Right - Epsilon &&
            a.Top < b.Bottom - Epsilon &&
            b.Top < a.Bottom - Epsilon;

        private static bool IsOnBorder(LayoutRect rect, LayoutPoint point)
        {
            var inside = point.X >= rect.Left - Epsilon && point.X <= rect.Right + Epsilon &&
                         point.Y >= rect.Top - Epsilon && point.Y <= rect.Bottom + Epsilon;

            var onEdge = Math.Abs(point.X - rect.Left) < Epsilon ||
                         Math.Abs(point.X - rect.Right) < Epsilon ||
                         Math.Abs(point.Y - rect.Top) < Epsilon ||
                         Math.Abs(point.Y - rect.Bottom) < Epsilon;

            return inside && onEdge;
        }

        // ---------------------------------------------------------------- structure

        [TestMethod]
        public void Layout_CreatesANodeForEveryProcessAndResource()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            Assert.AreEqual(4, layout.Nodes.Count);
            Assert.AreEqual(2, layout.Nodes.OfType<DeadlockProcessNode>().Count());
            Assert.AreEqual(2, layout.Nodes.OfType<DeadlockResourceNode>().Count());
        }

        [TestMethod]
        public void Layout_AlternatesProcessAndResourceAroundTheRing()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            for (var i = 0; i < layout.Nodes.Count; i++)
            {
                var expectProcess = i % 2 == 0;
                Assert.AreEqual(
                    expectProcess,
                    layout.Nodes[i] is DeadlockProcessNode,
                    $"Node {i} should be a {(expectProcess ? "process" : "resource")}.");
            }
        }

        [TestMethod]
        public void Layout_StartsAtTheVictim()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            var first = (DeadlockProcessNode)layout.Nodes[0];
            Assert.AreEqual(61, first.Process.Spid);
            Assert.IsTrue(first.IsVictim);
        }

        [TestMethod]
        public void Layout_PlacesTheVictimAtTheTop()
        {
            // The ring starts at twelve o'clock, so leading with the victim also puts it top of the
            // picture, which is where the eye goes first.
            var layout = LayoutSample(TestGraphs.KeyLock);

            var topmost = layout.Nodes.OrderBy(n => n.Bounds.Centre.Y).First();
            Assert.AreSame(layout.Nodes[0], topmost);
        }

        // ---------------------------------------------------------------- cycle

        [TestMethod]
        public void Layout_TracesTheDeadlockCycle()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            Assert.AreEqual(4, layout.Cycle.Count);
            CollectionAssert.AreEqual(layout.Nodes.ToArray(), layout.Cycle.ToArray());
        }

        [TestMethod]
        public void Layout_ThreeWayDeadlock_TracesASixNodeCycle()
        {
            var layout = LayoutSample(TestGraphs.ThreeWay);

            Assert.AreEqual(6, layout.Cycle.Count);
            CollectionAssert.AreEqual(
                new[] { 10, 20, 30 },
                layout.Cycle.OfType<DeadlockProcessNode>().Select(n => n.Process.Spid!.Value).ToArray());
        }

        [TestMethod]
        public void Layout_ParallelDeadlock_TracesTheCycle()
        {
            var layout = LayoutSample(TestGraphs.Parallel);

            Assert.AreEqual(4, layout.Cycle.Count);
            Assert.IsTrue(layout.Nodes.All(n => n.IsInCycle));
        }

        [TestMethod]
        public void Layout_ResourceWithSeveralOwners_TracesTheCycleThroughTheOwnerOnIt()
        {
            // Two readers hold S on the key the writer wants; only one of them is deadlocked, and it
            // is listed second.  Following the first owner leads off the cycle - which used to end
            // with a complete graph being reported as truncated.
            var layout = LayoutSample(TestGraphs.SharedLock);

            Assert.AreEqual(4, layout.Cycle.Count);
            CollectionAssert.AreEqual(
                new[] { 55, 62 },
                layout.Cycle.OfType<DeadlockProcessNode>().Select(n => n.Process.Spid!.Value).ToArray());
        }

        [TestMethod]
        public void Layout_ResourceWithSeveralOwners_LeavesTheUninvolvedOwnerOutOfTheCycle()
        {
            var layout = LayoutSample(TestGraphs.SharedLock);

            // The second reader holds the same lock but waits for nothing, so it is drawn - with its
            // owner arrow - but is no part of the deadlock.
            var reader = layout.Nodes.OfType<DeadlockProcessNode>().Single(n => n.Process.Spid == 71);
            Assert.IsFalse(reader.IsInCycle);

            var ownerEdge = layout.Edges.Single(e =>
                e.Kind == DeadlockEdgeKind.Owner && ReferenceEquals(e.To, reader));
            Assert.IsFalse(ownerEdge.IsInCycle);

            // Five arrows in all - three for the shared key, two for the other - and the four that
            // are not the uninvolved reader's belong to the cycle.
            Assert.AreEqual(5, layout.Edges.Count);
            Assert.AreEqual(4, layout.Edges.Count(e => e.IsInCycle));
        }

        [TestMethod]
        public void Layout_ConversionDeadlock_PutsBothSessionsInTheCycle()
        {
            // One resource, owned and waited on by both processes.  The cycle passes through the
            // resource twice, but a node can only be placed once, so the ring is a triangle.
            var layout = LayoutSample(TestGraphs.Conversion);

            Assert.AreEqual(3, layout.Cycle.Count);
            CollectionAssert.AreEqual(
                new[] { 58, 64 },
                layout.Cycle.OfType<DeadlockProcessNode>().Select(n => n.Process.Spid!.Value).ToArray());
            Assert.AreEqual(1, layout.Cycle.OfType<DeadlockResourceNode>().Count());
        }

        [TestMethod]
        public void Layout_TwoEdgesBetweenOnePair_AreDrawnApart()
        {
            // A process that owns and waits on the same resource gets two arrows between the same two
            // boxes.  Routed down the middle they land on the same line and read as one double headed
            // arrow, so they are offset to either side of it.
            var options = new DeadlockLayoutOptions { ParallelEdgeSpacing = 20 };
            var layout = LayoutSample(TestGraphs.Conversion, options);

            var victim = layout.Nodes.OfType<DeadlockProcessNode>().Single(n => n.Process.Spid == 58);
            var pair = layout.Edges.Where(e => ReferenceEquals(e.From, victim) || ReferenceEquals(e.To, victim))
                .ToArray();

            Assert.AreEqual(2, pair.Length);
            Assert.AreEqual(
                options.ParallelEdgeSpacing,
                DistanceBetweenLines(pair[0], pair[1]),
                0.5,
                "The two arrows between one pair of nodes should be a spacing apart.");
        }

        [TestMethod]
        public void Layout_TwoEdgesBetweenOnePair_SpaceTheirLabelsAlongTheLine()
        {
            // Side by side lines are only far enough apart to be told apart - not far enough to keep
            // two labels off each other - so the labels move along the line instead.
            var layout = LayoutSample(TestGraphs.Conversion);

            var victim = layout.Nodes.OfType<DeadlockProcessNode>().Single(n => n.Process.Spid == 58);
            var pair = layout.Edges.Where(e => ReferenceEquals(e.From, victim) || ReferenceEquals(e.To, victim))
                .ToArray();

            var apart = Distance(pair[0].LabelAnchor, pair[1].LabelAnchor);
            Assert.IsTrue(apart > 40, $"Labels on a shared pair should not sit on top of each other (gap {apart:F1}).");
        }

        [TestMethod]
        public void Layout_PairSharingEdges_IsGivenRoomToSpreadTheirLabels()
        {
            // Two arrows and two labels have to fit in the gap between the process and the key, so
            // the pair cannot be packed to the spacing an ordinary pair gets - the arrows come out
            // stubby and the labels pile up on each other and on the boxes.
            var layout = LayoutSample(TestGraphs.Conversion);
            var resource = layout.Nodes.OfType<DeadlockResourceNode>().Single();
            var ordinary = new DeadlockLayoutOptions().NodeSpacing;

            foreach (var process in layout.Nodes.OfType<DeadlockProcessNode>())
            {
                var gap = GapBetween(process.Bounds, resource.Bounds);
                Assert.IsTrue(
                    gap > ordinary * 2,
                    $"SPID {process.Process.Spid} sits {gap:F0} from the resource - too close for two labelled arrows.");
            }
        }

        [TestMethod]
        public void Layout_OrdinaryDeadlock_StillPacksToTheStandardSpacing()
        {
            // The extra room is only for pairs that share edges.  Every pair here has one, so the
            // ring must be exactly as tight as it was before that rule existed.
            var layout = LayoutSample(TestGraphs.KeyLock);
            var tightest = double.MaxValue;

            for (var i = 0; i < layout.Nodes.Count; i++)
            {
                for (var j = i + 1; j < layout.Nodes.Count; j++)
                {
                    tightest = Math.Min(tightest, GapBetween(layout.Nodes[i].Bounds, layout.Nodes[j].Bounds));
                }
            }

            Assert.AreEqual(new DeadlockLayoutOptions().NodeSpacing, tightest, 1.0);
        }

        [TestMethod]
        public void Layout_EdgeWithAPairToItself_KeepsItsLabelAtTheMidpoint()
        {
            // The offsets apply only where a pair is shared, so an ordinary deadlock is unchanged.
            var layout = LayoutSample(TestGraphs.KeyLock);

            foreach (var edge in layout.Edges)
            {
                Assert.AreEqual((edge.Start.X + edge.End.X) / 2, edge.LabelAnchor.X, Epsilon);
                Assert.AreEqual((edge.Start.Y + edge.End.Y) / 2, edge.LabelAnchor.Y, Epsilon);
            }
        }

        [TestMethod]
        public void Layout_ConversionDeadlock_MarksBothOwnerAndWaiterEdges()
        {
            var layout = LayoutSample(TestGraphs.Conversion);

            // Two owner arrows and two waiter arrows, and every one of them is part of the deadlock.
            Assert.AreEqual(4, layout.Edges.Count);
            Assert.IsTrue(layout.Edges.All(e => e.IsInCycle));
        }

        [TestMethod]
        public void Layout_MarksEveryCycleNode()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            Assert.IsTrue(layout.Nodes.All(n => n.IsInCycle));
        }

        [TestMethod]
        public void Layout_TruncatedGraph_ReportsNoCycle()
        {
            // The owner of the contended resource is missing from the process-list, so the walk
            // cannot close.  Reporting no cycle is better than reporting a wrong one.
            var layout = LayoutSample(TestGraphs.Truncated);

            Assert.AreEqual(0, layout.Cycle.Count);
            Assert.IsFalse(layout.Nodes.Any(n => n.IsInCycle));
            Assert.IsFalse(layout.Edges.Any(e => e.IsInCycle));
        }

        [TestMethod]
        public void Layout_TruncatedGraph_StillPlacesNodesTheWalkDidNotReach()
        {
            var layout = LayoutSample(TestGraphs.Truncated);

            Assert.AreEqual(3, layout.Nodes.Count);
            Assert.IsTrue(layout.Nodes.OfType<DeadlockResourceNode>()
                .Any(n => n.Resource.TypeName == "somethingNewInSql2030"));
        }

        // ---------------------------------------------------------------- edges

        [TestMethod]
        public void Layout_BuildsAnOwnerAndWaiterEdgePerResource()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            Assert.AreEqual(4, layout.Edges.Count);
            Assert.AreEqual(2, layout.Edges.Count(e => e.Kind == DeadlockEdgeKind.Owner));
            Assert.AreEqual(2, layout.Edges.Count(e => e.Kind == DeadlockEdgeKind.Waiter));
        }

        [TestMethod]
        public void Layout_OwnerEdgesPointFromResourceToProcess()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            foreach (var edge in layout.Edges.Where(e => e.Kind == DeadlockEdgeKind.Owner))
            {
                Assert.IsInstanceOfType<DeadlockResourceNode>(edge.From);
                Assert.IsInstanceOfType<DeadlockProcessNode>(edge.To);
            }
        }

        [TestMethod]
        public void Layout_WaiterEdgesPointFromProcessToResource()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            foreach (var edge in layout.Edges.Where(e => e.Kind == DeadlockEdgeKind.Waiter))
            {
                Assert.IsInstanceOfType<DeadlockProcessNode>(edge.From);
                Assert.IsInstanceOfType<DeadlockResourceNode>(edge.To);
            }
        }

        [TestMethod]
        public void Layout_EdgeLabelsCarryTheLockMode()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            Assert.IsTrue(layout.Edges.Where(e => e.Kind == DeadlockEdgeKind.Owner)
                .All(e => e.Label == "Owner: X"));
            Assert.IsTrue(layout.Edges.Where(e => e.Kind == DeadlockEdgeKind.Waiter)
                .All(e => e.Label == "Requested: U"));
        }

        [TestMethod]
        public void Layout_EdgeLabelOmitsModeWhenTheGraphHasNone()
        {
            // exchangeEvent owners and waiters carry no mode.
            var layout = LayoutSample(TestGraphs.Parallel);

            Assert.IsTrue(layout.Edges.Any(e => e.Label == "Owner"));
            Assert.IsTrue(layout.Edges.Any(e => e.Label == "Requested"));
        }

        [TestMethod]
        public void Layout_MarksEdgesThatFormTheCycle()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            Assert.IsTrue(layout.Edges.All(e => e.IsInCycle));
        }

        [TestMethod]
        public void Layout_TruncatedGraph_SkipsEdgesForUnresolvedParticipants()
        {
            var layout = LayoutSample(TestGraphs.Truncated);

            // The rid lock's owner is absent from the process-list, so only its waiter edge and the
            // unknown resource's owner edge survive.
            Assert.AreEqual(2, layout.Edges.Count);
            Assert.IsTrue(layout.Edges.All(e => e.From is not null && e.To is not null));
        }

        [TestMethod]
        public void Layout_EdgeEndpointsAreClippedToTheNodeBorders()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            foreach (var edge in layout.Edges)
            {
                Assert.IsTrue(
                    IsOnBorder(edge.From.Bounds, edge.Start),
                    $"Edge start {edge.Start} is not on the border of {edge.From.Bounds}.");
                Assert.IsTrue(
                    IsOnBorder(edge.To.Bounds, edge.End),
                    $"Edge end {edge.End} is not on the border of {edge.To.Bounds}.");
            }
        }

        [TestMethod]
        public void Layout_EdgeLabelAnchorIsTheMidpointOfTheRoutedLine()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            foreach (var edge in layout.Edges)
            {
                Assert.AreEqual((edge.Start.X + edge.End.X) / 2, edge.LabelAnchor.X, Epsilon);
                Assert.AreEqual((edge.Start.Y + edge.End.Y) / 2, edge.LabelAnchor.Y, Epsilon);
            }
        }

        // ---------------------------------------------------------------- layout styles

        [TestMethod]
        public void Layout_OffCycleNode_IsPlacedBesideWhatItHangsOff()
        {
            // The second S-lock holder is not deadlocked, so it does not take a slot on the ring.  It
            // used to, which stretched the ring by a node and dragged its one edge back across the
            // middle of the picture.
            var layout = LayoutSample(TestGraphs.SharedLock);

            var reader = layout.Nodes.OfType<DeadlockProcessNode>().Single(n => n.Process.Spid == 71);
            var key = layout.Nodes.OfType<DeadlockResourceNode>().Single(n => n.Resource.ObjectName == "Ops.dbo.Customer");

            var toKey = Distance(reader.Bounds.Centre, key.Bounds.Centre);
            var nearest = layout.Nodes
                .Where(n => !ReferenceEquals(n, reader))
                .Min(n => Distance(reader.Bounds.Centre, n.Bounds.Centre));

            Assert.AreEqual(nearest, toKey, Epsilon,
                "The node outside the cycle should sit nearer the node it is joined to than to anything else.");
        }

        [TestMethod]
        public void Layout_OffCycleNode_LeavesTheCycleAsItsOwnShape()
        {
            // Four nodes in the cycle means a diamond, whether or not something else hangs off it.
            var layout = LayoutSample(TestGraphs.SharedLock);
            var cycle = layout.Cycle.ToList();
            var centre = new LayoutPoint(cycle.Average(n => n.Bounds.Centre.X), cycle.Average(n => n.Bounds.Centre.Y));

            var radii = cycle.Select(n => Distance(centre, n.Bounds.Centre)).ToList();
            Assert.AreEqual(radii.Min(), radii.Max(), 1.0, "Cycle nodes should all sit on one ring.");

            var reader = layout.Nodes.Single(n => !cycle.Contains(n));
            Assert.IsTrue(
                Distance(centre, reader.Bounds.Centre) > radii.Max(),
                "A node outside the cycle belongs outside the ring, not on it.");
        }

        [TestMethod]
        public void Layout_Layered_ReadsFromTheVictimOutwards()
        {
            // SSMS style: the victim on the left, then what it is waiting for, then who is holding it.
            var layout = LayoutXml(TestGraphs.Load(TestGraphs.SharedLock),
                new DeadlockLayoutOptions { Style = DeadlockLayoutStyle.Layered });

            double CentreX(int spid) => layout.Nodes.OfType<DeadlockProcessNode>()
                .Single(n => n.Process.Spid == spid).Bounds.Centre.X;

            var resources = layout.Nodes.OfType<DeadlockResourceNode>().Select(n => n.Bounds.Centre.X).ToList();

            Assert.IsTrue(resources.All(x => x > CentreX(55)), "The victim leads, so the keys come after it.");
            Assert.IsTrue(resources.All(x => x < CentreX(62)), "The holders come after the keys they hold.");
            Assert.IsTrue(resources.All(x => x < CentreX(71)), "The holders come after the keys they hold.");
        }

        [TestMethod]
        public void Layout_Layered_PutsNodesOfOneLayerInAColumn()
        {
            var layout = LayoutXml(TestGraphs.Load(TestGraphs.SharedLock),
                new DeadlockLayoutOptions { Style = DeadlockLayoutStyle.Layered });

            // Both keys are one hop from the victim, so they share a column - same centre, stacked.
            var keys = layout.Nodes.OfType<DeadlockResourceNode>().ToList();

            Assert.AreEqual(2, keys.Count);
            Assert.AreEqual(keys[0].Bounds.Centre.X, keys[1].Bounds.Centre.X, Epsilon);
            Assert.AreNotEqual(keys[0].Bounds.Centre.Y, keys[1].Bounds.Centre.Y);
        }

        [TestMethod]
        [DataRow(TestGraphs.KeyLock)]
        [DataRow(TestGraphs.ThreeWay)]
        [DataRow(TestGraphs.SharedLock)]
        [DataRow(TestGraphs.Conversion)]
        [DataRow(TestGraphs.Truncated)]
        public void Layout_Layered_NodesDoNotOverlap(string sampleName)
        {
            var layout = LayoutXml(TestGraphs.Load(sampleName),
                new DeadlockLayoutOptions { Style = DeadlockLayoutStyle.Layered });

            for (var i = 0; i < layout.Nodes.Count; i++)
            {
                for (var j = i + 1; j < layout.Nodes.Count; j++)
                {
                    Assert.IsFalse(
                        Overlaps(layout.Nodes[i].Bounds, layout.Nodes[j].Bounds),
                        $"{layout.Nodes[i].Title} overlaps {layout.Nodes[j].Title}.");
                }
            }
        }

        [TestMethod]
        [DataRow(TestGraphs.SharedLock, DeadlockLayoutStyle.Ring)]
        [DataRow(TestGraphs.SharedLock, DeadlockLayoutStyle.Layered)]
        [DataRow(TestGraphs.ThreeWay, DeadlockLayoutStyle.Layered)]
        [DataRow(TestGraphs.Conversion, DeadlockLayoutStyle.Ring)]
        [DataRow(TestGraphs.Conversion, DeadlockLayoutStyle.Layered)]
        public void Layout_LabelsDoNotOverlapEachOther(string sampleName, DeadlockLayoutStyle style)
        {
            // Any two edges that cross put their labels in the same place, and one ends up hidden -
            // which reads as the surviving label belonging to both arrows.  A hidden "Owner: S" beside
            // a visible "Requested: U" says a shared lock is an update lock, so this matters more than
            // tidiness.
            var layout = LayoutXml(TestGraphs.Load(sampleName), new DeadlockLayoutOptions { Style = style });

            var labels = layout.Edges
                .Where(e => !string.IsNullOrEmpty(e.Label))
                .Select(e => (e.Label, Rect: LabelRect(e)))
                .ToList();

            for (var i = 0; i < labels.Count; i++)
            {
                for (var j = i + 1; j < labels.Count; j++)
                {
                    Assert.IsFalse(
                        Overlaps(labels[i].Rect, labels[j].Rect),
                        $"'{labels[i].Label}' {labels[i].Rect} overlaps '{labels[j].Label}' {labels[j].Rect}.");
                }
            }
        }

        /// <summary>
        /// The space a label occupies, matching what the engine reserves for it: the measured text
        /// plus the margin the renderer draws around it.
        /// </summary>
        private static LayoutRect LabelRect(DeadlockEdge edge) =>
            LayoutRect.FromCentre(
                edge.LabelAnchor,
                new LayoutSize(
                    (edge.Label.Length * FakeTextMeasurer.DetailCharWidth) + 8,
                    FakeTextMeasurer.DetailHeight + 4));

        // ---------------------------------------------------------------- geometry

        [TestMethod]
        [DataRow(TestGraphs.KeyLock)]
        [DataRow(TestGraphs.ThreeWay)]
        [DataRow(TestGraphs.Parallel)]
        [DataRow(TestGraphs.DeadlockListWrapper)]
        [DataRow(TestGraphs.Truncated)]
        [DataRow(TestGraphs.SharedLock)]
        [DataRow(TestGraphs.Conversion)]
        public void Layout_NodesDoNotOverlap(string sampleName)
        {
            // Chord length between ring neighbours is not on its own enough to separate axis aligned
            // rectangles - two nodes are clear when separated on either axis, and the radius has to
            // be derived from that.  This is the check that keeps that calculation honest.
            var layout = LayoutSample(sampleName);

            for (var i = 0; i < layout.Nodes.Count; i++)
            {
                for (var j = i + 1; j < layout.Nodes.Count; j++)
                {
                    Assert.IsFalse(
                        Overlaps(layout.Nodes[i].Bounds, layout.Nodes[j].Bounds),
                        $"{layout.Nodes[i].Title} {layout.Nodes[i].Bounds} overlaps " +
                        $"{layout.Nodes[j].Title} {layout.Nodes[j].Bounds}.");
                }
            }
        }

        [TestMethod]
        [DataRow(TestGraphs.KeyLock)]
        [DataRow(TestGraphs.ThreeWay)]
        [DataRow(TestGraphs.Parallel)]
        public void Layout_BoundsStartAtTheOrigin(string sampleName)
        {
            var layout = LayoutSample(sampleName);

            Assert.AreEqual(0, layout.Bounds.X, Epsilon);
            Assert.AreEqual(0, layout.Bounds.Y, Epsilon);
        }

        [TestMethod]
        [DataRow(TestGraphs.KeyLock)]
        [DataRow(TestGraphs.ThreeWay)]
        [DataRow(TestGraphs.Parallel)]
        public void Layout_EveryNodeSitsInsideTheBoundsAtNonNegativeCoordinates(string sampleName)
        {
            var layout = LayoutSample(sampleName);

            foreach (var node in layout.Nodes)
            {
                Assert.IsTrue(node.Bounds.Left >= -Epsilon, $"{node.Title} has a negative X.");
                Assert.IsTrue(node.Bounds.Top >= -Epsilon, $"{node.Title} has a negative Y.");
                Assert.IsTrue(node.Bounds.Right <= layout.Bounds.Right + Epsilon);
                Assert.IsTrue(node.Bounds.Bottom <= layout.Bounds.Bottom + Epsilon);
            }
        }

        [TestMethod]
        public void Layout_LeavesTheConfiguredMarginAroundTheContent()
        {
            var options = new DeadlockLayoutOptions { Margin = 40 };
            var layout = LayoutSample(TestGraphs.KeyLock, options);

            Assert.AreEqual(40, layout.Nodes.Min(n => n.Bounds.Left), Epsilon);
            Assert.AreEqual(40, layout.Nodes.Min(n => n.Bounds.Top), Epsilon);
            Assert.AreEqual(40, layout.Bounds.Right - layout.Nodes.Max(n => n.Bounds.Right), Epsilon);
        }

        // ---------------------------------------------------------------- sizing

        [TestMethod]
        public void Layout_NodeIsNeverNarrowerThanTheMinimum()
        {
            var options = new DeadlockLayoutOptions { MinNodeWidth = 150 };
            var layout = LayoutXml(
                """
                <deadlock><process-list><process id="p1" spid="5" /></process-list></deadlock>
                """,
                options);

            Assert.AreEqual(150, layout.Nodes.Single().Bounds.Width, Epsilon);
        }

        [TestMethod]
        public void Layout_NodeIsNeverWiderThanTheMaximum()
        {
            // A three part object name can be very long; clamping keeps it from stretching the ring,
            // and eliding the text is left to the renderer.
            var longName = new string('x', 200);
            var options = new DeadlockLayoutOptions { MaxNodeWidth = 300 };
            var layout = LayoutXml(
                $"""
                 <deadlock><resource-list><keylock objectname="{longName}" mode="X" /></resource-list></deadlock>
                 """,
                options);

            Assert.AreEqual(300, layout.Nodes.Single().Bounds.Width, Epsilon);
        }

        [TestMethod]
        public void Layout_NodeGrowsWithItsText()
        {
            var shortTitle = LayoutXml(
                """
                <deadlock><resource-list><keylock objectname="db.dbo.T" /></resource-list></deadlock>
                """).Nodes.Single();

            var longTitle = LayoutXml(
                """
                <deadlock><resource-list><keylock objectname="db.dbo.AVeryMuchLongerTableName" /></resource-list></deadlock>
                """).Nodes.Single();

            Assert.IsTrue(
                longTitle.Bounds.Width > shortTitle.Bounds.Width,
                "A longer title should produce a wider node.");
        }

        // ---------------------------------------------------------------- labels

        [TestMethod]
        public void Layout_ProcessNodeShowsDatabaseLoginApplicationAndActivity()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);
            var victim = layout.Nodes.OfType<DeadlockProcessNode>().Single(n => n.Process.Spid == 61);

            Assert.AreEqual("SPID 61", victim.Title);
            CollectionAssert.AreEqual(
                new[] { "Sales", "CONTOSO\\svc_app", "DBA Dash", "Waited 1842 ms | Log Used 288 bytes" },
                victim.DetailLines.ToArray());
        }

        [TestMethod]
        public void Layout_WaitAndLogUsedShareOneLine()
        {
            // A node shows only a few detail lines, and these two earn one between them: together
            // they say whether this is a reader holding shared locks or a transaction with a lot to
            // roll back.
            var lines = DetailLinesOf(
                """
                <deadlock><process-list><process id="p1" spid="5" waittime="400" logused="200" /></process-list></deadlock>
                """);

            CollectionAssert.Contains(lines, "Waited 400 ms | Log Used 200 bytes");
        }

        [TestMethod]
        public void Layout_ActivityLineOmitsWhicheverTheGraphDoesNotCarry()
        {
            CollectionAssert.Contains(
                DetailLinesOf(
                    """
                    <deadlock><process-list><process id="p1" spid="5" logused="200" /></process-list></deadlock>
                    """),
                "Log Used 200 bytes");

            CollectionAssert.Contains(
                DetailLinesOf(
                    """
                    <deadlock><process-list><process id="p1" spid="5" waittime="400" /></process-list></deadlock>
                    """),
                "Waited 400 ms");
        }

        [TestMethod]
        public void Layout_LargeLogUsedIsShownInLargerUnits()
        {
            // A transaction can burn megabytes, and the node has nowhere near the room for the digits.
            // The exact byte count is still a tooltip away.
            CollectionAssert.Contains(
                DetailLinesOf(
                    """
                    <deadlock><process-list><process id="p1" spid="5" logused="5242880" /></process-list></deadlock>
                    """),
                "Log Used 5 MB");
        }

        private static string[] DetailLinesOf(string xml) =>
            LayoutXml(xml).Nodes.Single().DetailLines.ToArray();

        [TestMethod]
        public void Layout_LongWaitIsShownInSeconds()
        {
            var layout = LayoutXml(
                """
                <deadlock><process-list><process id="p1" spid="5" waittime="15000" /></process-list></deadlock>
                """);

            CollectionAssert.Contains(layout.Nodes.Single().DetailLines.ToArray(), "Waited 15 s");
        }

        [TestMethod]
        public void Layout_ResourceNodeShowsItsTypeAndMode()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);
            var orders = layout.Nodes.OfType<DeadlockResourceNode>()
                .Single(n => n.Resource.ObjectName == "Sales.dbo.Orders");

            // No database on the title: this graph is inside one, so naming it on every box would
            // only crowd out the table.
            Assert.AreEqual("dbo.Orders (PK_Orders)", orders.Title);
            CollectionAssert.AreEqual(new[] { "keylock", "Mode: X" }, orders.DetailLines.ToArray());
        }

        /// <summary>
        /// Across databases the name is the point rather than the noise, so it comes back - on its own
        /// line, where a long one cannot push the table name out of the box.
        /// </summary>
        [TestMethod]
        public void Layout_ResourceNodeShowsTheDatabaseOnlyWhenTheGraphSpansMoreThanOne()
        {
            var layout = LayoutXml(
                """
                <deadlock>
                  <process-list>
                    <process id="p1" spid="55" />
                    <process id="p2" spid="62" />
                  </process-list>
                  <resource-list>
                    <keylock objectname="Sales.dbo.Orders" indexname="PK_Orders" id="lock1" mode="X">
                      <owner-list><owner id="p1" mode="X" /></owner-list>
                      <waiter-list><waiter id="p2" mode="S" requestType="wait" /></waiter-list>
                    </keylock>
                    <keylock objectname="Warehouse.dbo.Stock" indexname="PK_Stock" id="lock2" mode="X">
                      <owner-list><owner id="p2" mode="X" /></owner-list>
                      <waiter-list><waiter id="p1" mode="S" requestType="wait" /></waiter-list>
                    </keylock>
                  </resource-list>
                </deadlock>
                """);

            var orders = layout.Nodes.OfType<DeadlockResourceNode>()
                .Single(n => n.Resource.ObjectName == "Sales.dbo.Orders");

            Assert.AreEqual("dbo.Orders (PK_Orders)", orders.Title);
            CollectionAssert.Contains(orders.DetailLines.ToArray(), "Sales");
        }

        /// <summary>
        /// A page lock is titled by its page and shows the object underneath.  The other way round -
        /// the object on the title - gives every page lock of one table an identical heading, and a
        /// long name is elided to fit the box, so the part that differs is the part that is lost.
        /// </summary>
        [TestMethod]
        public void Layout_PageLockNodeIsTitledByItsPageWithTheObjectBeneath()
        {
            var layout = LayoutSample(TestGraphs.ParallelPageLocks);
            var page = layout.Nodes.OfType<DeadlockResourceNode>().Single(n => n.Resource.PageId == 900);

            Assert.AreEqual("Page 6:1:900", page.Title);
            CollectionAssert.AreEqual(
                new[] { "dbo.Attachment", "pagelock", "Mode: U" }, page.DetailLines.ToArray());
        }

        [TestMethod]
        public void Layout_ParallelismResourceShowsItsWaitType()
        {
            // An exchangeEvent has no object name or mode, so the wait type is the useful detail.
            var layout = LayoutSample(TestGraphs.Parallel);
            var pipe = layout.Nodes.OfType<DeadlockResourceNode>().First();

            Assert.AreEqual("exchangeEvent", pipe.Title);
            CollectionAssert.Contains(pipe.DetailLines.ToArray(), "e_waitPipeNewRow");
        }

        [TestMethod]
        public void Layout_LimitsDetailLinesToTheConfiguredMaximum()
        {
            var options = new DeadlockLayoutOptions { MaxDetailLines = 2 };
            var layout = LayoutSample(TestGraphs.KeyLock, options);
            var victim = layout.Nodes.OfType<DeadlockProcessNode>().Single(n => n.Process.Spid == 61);

            Assert.AreEqual(2, victim.DetailLines.Count);
        }

        // ---------------------------------------------------------------- degenerate input

        [TestMethod]
        public void Layout_EmptyDeadlock_ProducesAnEmptyLayout()
        {
            var layout = LayoutXml(
                """
                <deadlock><victim-list /><process-list /><resource-list /></deadlock>
                """);

            Assert.AreEqual(0, layout.Nodes.Count);
            Assert.AreEqual(0, layout.Edges.Count);
            Assert.AreEqual(0, layout.Cycle.Count);
            Assert.AreEqual(new LayoutRect(0, 0, 0, 0), layout.Bounds);
        }

        [TestMethod]
        public void Layout_SingleNode_IsPlacedWithinTheBounds()
        {
            var layout = LayoutXml(
                """
                <deadlock><process-list><process id="p1" spid="5" /></process-list></deadlock>
                """);

            var node = layout.Nodes.Single();
            Assert.IsTrue(node.Bounds.Left >= -Epsilon);
            Assert.IsTrue(node.Bounds.Top >= -Epsilon);
            Assert.IsTrue(layout.Bounds.Width > node.Bounds.Width);
        }

        [TestMethod]
        public void Layout_NullGraph_Throws()
        {
            var engine = new DeadlockLayoutEngine(new FakeTextMeasurer());

            Assert.ThrowsExactly<ArgumentNullException>(() => engine.Layout(null!));
        }

        [TestMethod]
        public void Constructor_NullMeasurer_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DeadlockLayoutEngine(null!));
        }

        // ---------------------------------------------------------------- hit testing

        [TestMethod]
        public void HitTest_FindsTheNodeUnderThePoint()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);
            var target = layout.Nodes[2];

            Assert.AreSame(target, layout.HitTest(target.Bounds.Centre));
        }

        [TestMethod]
        public void HitTest_ReturnsNullInsideTheBoundsButAwayFromAnyNode()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            // Normalisation puts the leftmost and topmost node edges exactly one margin in, so any
            // point inside the margin is within the bounds but on no node.  (The centre of the
            // bounds is not a safe choice: the union is asymmetric when node widths differ, so it
            // can legitimately land on a node.)
            var inMargin = new LayoutPoint(1, 1);

            Assert.IsTrue(layout.Bounds.Contains(inMargin));
            Assert.IsNull(layout.HitTest(inMargin));
        }

        [TestMethod]
        public void HitTest_ReturnsNullOutsideTheLayout()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);

            Assert.IsNull(layout.HitTest(new LayoutPoint(-100, -100)));
        }
        // ---------------------------------------------------------------- moving nodes

        /// <summary>
        /// Re-routing is the engine's job, so a layout that did not come from one cannot honour a
        /// move: it would leave the arrows pointing at where the node used to be, which is worse than
        /// refusing.
        /// </summary>
        [TestMethod]
        public void MoveNode_OnALayoutTheEngineDidNotProduce_Throws()
        {
            var layout = new DeadlockLayout();

            Assert.ThrowsExactly<InvalidOperationException>(
                () => layout.MoveNode(new DeadlockProcessNode(), 10, 10));
        }

        [TestMethod]
        public void MoveNode_ByNothing_ChangesNothing()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);
            var node = layout.Nodes[0];
            var before = node.Bounds;

            layout.MoveNode(node, 0, 0);

            Assert.AreEqual(before, node.Bounds);
        }

        [TestMethod]
        public void BringToFront_MakesTheNodeTheLastPaintedAndTheFirstHitTested()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);
            var node = layout.Nodes[0];
            var count = layout.Nodes.Count;

            layout.BringToFront(node);

            Assert.AreSame(node, layout.Nodes[^1]);
            Assert.AreEqual(count, layout.Nodes.Count);
            Assert.AreSame(node, layout.HitTest(node.Bounds.Centre));
        }

        [TestMethod]
        public void BringToFront_IgnoresANodeThatIsNotInTheLayout()
        {
            var layout = LayoutSample(TestGraphs.KeyLock);
            var before = layout.Nodes.ToArray();

            layout.BringToFront(new DeadlockProcessNode());

            CollectionAssert.AreEqual(before, layout.Nodes.ToArray());
        }

    }
}
