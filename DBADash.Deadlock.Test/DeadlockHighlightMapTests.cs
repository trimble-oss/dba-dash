using System.Linq;
using DBADash.Deadlock.Interaction;
using DBADash.Deadlock.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// What selecting a node emphasises: what it holds, what it is waiting for, and which other boxes
    /// are the same session.  This is the part that makes a graph with dozens of nodes readable, so it
    /// is worked out here rather than in the renderer, where it could only be checked by eye.
    /// </summary>
    [TestClass]
    public class DeadlockHighlightMapTests
    {
        private static DeadlockLayout Layout(string sampleName)
        {
            var graph = DeadlockParser.Parse(TestGraphs.Load(sampleName)).First();
            return new DeadlockLayoutEngine(new FakeTextMeasurer()).Layout(graph);
        }

        private static DeadlockProcessNode Process(DeadlockLayout layout, int spid, int ecid = 0) =>
            layout.Nodes
                .OfType<DeadlockProcessNode>()
                .Single(n => n.Process.Spid == spid && (n.Process.Ecid ?? 0) == ecid);

        private static DeadlockResourceNode Resource(DeadlockLayout layout, string objectName) =>
            layout.Nodes
                .OfType<DeadlockResourceNode>()
                .Single(n => n.Resource.ObjectName == objectName);

        // ---------------------------------------------------------------- nothing selected

        [TestMethod]
        public void Empty_IsNotActive()
        {
            Assert.IsFalse(DeadlockHighlightMap.Empty.IsActive);
            Assert.IsNull(DeadlockHighlightMap.Empty.Focus);
        }

        [TestMethod]
        public void For_NullFocus_GivesTheEmptyMap()
        {
            var layout = Layout(TestGraphs.SharedLock);

            var map = DeadlockHighlightMap.For(layout, null);

            Assert.AreSame(DeadlockHighlightMap.Empty, map);
            Assert.AreEqual(DeadlockHighlightRole.None, map.RoleOf(layout.Nodes[0]));
        }

        /// <summary>
        /// A node from another layout has no edges here to trace, so it must not put the view into a
        /// state where everything is faded and nothing is emphasised.
        /// </summary>
        [TestMethod]
        public void For_NodeFromAnotherLayout_GivesTheEmptyMap()
        {
            var layout = Layout(TestGraphs.SharedLock);
            var other = Layout(TestGraphs.SharedLock);

            var map = DeadlockHighlightMap.For(layout, other.Nodes[0]);

            Assert.AreSame(DeadlockHighlightMap.Empty, map);
        }

        // ---------------------------------------------------------------- a process is selected

        [TestMethod]
        public void For_ProcessNode_MarksItAsTheFocus()
        {
            var layout = Layout(TestGraphs.SharedLock);
            var focus = Process(layout, 55);

            var map = DeadlockHighlightMap.For(layout, focus);

            Assert.IsTrue(map.IsActive);
            Assert.AreSame(focus, map.Focus);
            Assert.AreEqual(DeadlockHighlightRole.Focus, map.RoleOf(focus));
        }

        [TestMethod]
        public void For_ProcessNode_MarksWhatItHoldsAsOwned()
        {
            var layout = Layout(TestGraphs.SharedLock);

            // SPID 55 holds the Invoice key and is blocked on the Customer key.
            var map = DeadlockHighlightMap.For(layout, Process(layout, 55));

            Assert.AreEqual(DeadlockHighlightRole.Owns, map.RoleOf(Resource(layout, "Ops.dbo.Invoice")));
            CollectionAssert.AreEquivalent(
                new[] { Resource(layout, "Ops.dbo.Invoice") },
                map.OwnershipRelated.ToArray());
        }

        [TestMethod]
        public void For_ProcessNode_MarksWhatItIsBlockedOnAsWanted()
        {
            var layout = Layout(TestGraphs.SharedLock);

            var map = DeadlockHighlightMap.For(layout, Process(layout, 55));

            Assert.AreEqual(DeadlockHighlightRole.Wants, map.RoleOf(Resource(layout, "Ops.dbo.Customer")));
            CollectionAssert.AreEquivalent(
                new[] { Resource(layout, "Ops.dbo.Customer") },
                map.WaitRelated.ToArray());
        }

        /// <summary>
        /// SPID 71 shares the Customer key with SPID 62 and has nothing to do with the Invoice key.
        /// Everything it is not connected to has to come back as None, because that is what the
        /// renderer fades.
        /// </summary>
        [TestMethod]
        public void For_ProcessNode_LeavesUnconnectedNodesUnmarked()
        {
            var layout = Layout(TestGraphs.SharedLock);

            var map = DeadlockHighlightMap.For(layout, Process(layout, 71));

            Assert.AreEqual(DeadlockHighlightRole.None, map.RoleOf(Resource(layout, "Ops.dbo.Invoice")));
            Assert.AreEqual(DeadlockHighlightRole.None, map.RoleOf(Process(layout, 55)));
            Assert.AreEqual(0, map.WaitRelated.Count);
        }

        // ---------------------------------------------------------------- a resource is selected

        [TestMethod]
        public void For_ResourceNode_MarksItsOwnersAndItsWaiters()
        {
            var layout = Layout(TestGraphs.SharedLock);

            // The Customer key is held S by SPIDs 71 and 62, with SPID 55 waiting for X.
            var map = DeadlockHighlightMap.For(layout, Resource(layout, "Ops.dbo.Customer"));

            Assert.AreEqual(DeadlockHighlightRole.Owns, map.RoleOf(Process(layout, 71)));
            Assert.AreEqual(DeadlockHighlightRole.Owns, map.RoleOf(Process(layout, 62)));
            Assert.AreEqual(DeadlockHighlightRole.Wants, map.RoleOf(Process(layout, 55)));

            CollectionAssert.AreEquivalent(
                new[] { Process(layout, 71), Process(layout, 62) },
                map.OwnershipRelated.ToArray());
            CollectionAssert.AreEquivalent(new[] { Process(layout, 55) }, map.WaitRelated.ToArray());
        }

        // ---------------------------------------------------------------- both at once

        /// <summary>
        /// A lock conversion has the same process on both lists - holding S while asking for X is how
        /// the deadlock happened.  Both facts are kept, but the node is drawn as an owner: who holds
        /// what is the question the graph is being read to answer.
        /// </summary>
        [TestMethod]
        public void For_Conversion_KeepsBothRelationshipsButDrawsTheProcessAsAnOwner()
        {
            var layout = Layout(TestGraphs.Conversion);
            var map = DeadlockHighlightMap.For(layout, Resource(layout, "Ops.dbo.Ledger"));

            var process = Process(layout, 58);

            Assert.AreEqual(DeadlockHighlightRole.Owns, map.RoleOf(process));
            CollectionAssert.Contains(map.OwnershipRelated.ToArray(), process);
            CollectionAssert.Contains(map.WaitRelated.ToArray(), process);
        }

        // ---------------------------------------------------------------- parallel queries

        /// <summary>
        /// The eight boxes of one parallel query are one participant.  Marking the siblings keeps them
        /// out of the fade, so selecting a task does not hide the rest of its own SPID.
        /// </summary>
        [TestMethod]
        public void For_ParallelTask_MarksTheOtherTasksOfTheSameSpid()
        {
            var layout = Layout(TestGraphs.Parallel);

            var map = DeadlockHighlightMap.For(layout, Process(layout, 88));

            Assert.AreEqual(DeadlockHighlightRole.Session, map.RoleOf(Process(layout, 88, 3)));
        }

        // ---------------------------------------------------------------- edges

        [TestMethod]
        public void RoleOf_Edge_FollowsTheRelationshipItDraws()
        {
            var layout = Layout(TestGraphs.SharedLock);
            var focus = Process(layout, 55);

            var map = DeadlockHighlightMap.For(layout, focus);

            foreach (var edge in layout.Edges)
            {
                var touchesFocus = ReferenceEquals(edge.From, focus) || ReferenceEquals(edge.To, focus);

                var expected = !touchesFocus
                    ? DeadlockHighlightRole.None
                    : edge.Kind == DeadlockEdgeKind.Owner
                        ? DeadlockHighlightRole.Owns
                        : DeadlockHighlightRole.Wants;

                Assert.AreEqual(expected, map.RoleOf(edge), $"{edge.Kind} edge {edge.From.Title} -> {edge.To.Title}");
            }
        }

        // ---------------------------------------------------------------- the controller

        [TestMethod]
        public void Controller_RebuildsTheHighlightWithTheSelection()
        {
            var layout = Layout(TestGraphs.SharedLock);
            var controller = new DeadlockViewController(layout);

            Assert.IsFalse(controller.Highlight.IsActive);

            controller.Select(Process(layout, 55));
            Assert.AreSame(Process(layout, 55), controller.Highlight.Focus);
            Assert.AreEqual(
                DeadlockHighlightRole.Owns, controller.Highlight.RoleOf(Resource(layout, "Ops.dbo.Invoice")));

            controller.ClearSelection();
            Assert.IsFalse(controller.Highlight.IsActive);
        }
    }
}
