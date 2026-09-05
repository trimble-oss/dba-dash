using System;
using System.Collections.Generic;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock.Layout
{
    /// <summary>
    /// A deadlock graph with everything positioned: nodes with bounds, edges routed between them,
    /// and the overall extent.  Produced by <see cref="DeadlockLayoutEngine"/> and consumed by the
    /// renderer and the interaction layer.
    ///
    /// Coordinates are normalised so a freshly laid out graph starts at the origin, which saves
    /// every renderer from compensating for negative coordinates.  Moving a node by hand can take
    /// them negative - the alternative, shifting every other node to compensate, would move the whole
    /// graph out from under the reader - so consumers take the origin from <see cref="Bounds"/>
    /// rather than assuming it is zero.
    /// </summary>
    public sealed class DeadlockLayout
    {
        /// <summary>The graph this layout was produced from.</summary>
        public DeadlockGraph Graph { get; internal set; } = null!;

        public IReadOnlyList<DeadlockNode> Nodes { get; internal set; } = Array.Empty<DeadlockNode>();

        public IReadOnlyList<DeadlockEdge> Edges { get; internal set; } = Array.Empty<DeadlockEdge>();

        /// <summary>
        /// The nodes forming the deadlock cycle, alternating process and resource, in cycle order.
        /// Empty when no closed cycle could be traced - which happens with truncated graphs where a
        /// resource owner is missing from the process-list.
        /// </summary>
        public IReadOnlyList<DeadlockNode> Cycle { get; internal set; } = Array.Empty<DeadlockNode>();

        /// <summary>
        /// The extent of the layout including the configured margin.  Starts at (0, 0) as laid out;
        /// once a node has been moved by hand it starts wherever the nodes now do, which may be
        /// negative - see <see cref="MoveNode"/>.  Consumers must use its origin rather than assume
        /// zero.
        /// </summary>
        public LayoutRect Bounds { get; internal set; }

        /// <summary>
        /// The engine that produced this layout, kept so a moved node can have its edges routed
        /// again without laying the whole graph out from scratch.
        /// </summary>
        internal DeadlockLayoutEngine? Router { get; set; }

        /// <summary>
        /// Move one node by a distance in layout space, re-routing the edges that meet it and
        /// re-measuring the extent.  Everything else stays exactly where the reader put it.
        ///
        /// This is what lets a reader pull an overlapping box out of a pile-up - the graph is laid
        /// out mechanically, and on a busy deadlock no mechanical arrangement is right for every
        /// question being asked of it.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The layout was not produced by <see cref="DeadlockLayoutEngine"/>, so its edges cannot be
        /// routed again - moving a node would leave the arrows pointing at where it used to be.
        /// </exception>
        public void MoveNode(DeadlockNode node, double dx, double dy)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (Router is null)
            {
                throw new InvalidOperationException(
                    "This layout was not produced by a DeadlockLayoutEngine, so its edges cannot be re-routed.");
            }

            if (dx == 0 && dy == 0) return;

            node.Bounds = node.Bounds.Offset(dx, dy);
            Router.Reroute(this);
        }

        /// <summary>
        /// The padding the engine sized nodes with.  Carried on the layout so a renderer places text
        /// exactly where the node was measured for it, rather than keeping its own copy of the value
        /// that could drift out of step.
        /// </summary>
        public double NodePadding { get; internal set; }

        /// <summary>The gap the engine left between text lines inside a node.</summary>
        public double LineSpacing { get; internal set; }

        /// <summary>
        /// Move a node to the end of <see cref="Nodes"/>, so it is painted last and hit tested first.
        ///
        /// Called when a node is picked up: a box dropped on top of another has to be the one that is
        /// drawn on top and the one the next click finds, or the reader has moved it somewhere they
        /// cannot pick it up from again.
        /// </summary>
        public void BringToFront(DeadlockNode node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (Nodes.Count == 0 || ReferenceEquals(Nodes[^1], node)) return;

            var reordered = new List<DeadlockNode>(Nodes.Count);
            foreach (var other in Nodes)
            {
                if (!ReferenceEquals(other, node)) reordered.Add(other);
            }

            // Only reorder a node that is actually ours; anything else would silently add it.
            if (reordered.Count == Nodes.Count) return;

            reordered.Add(node);
            Nodes = reordered;
        }

        /// <summary>
        /// The topmost node containing <paramref name="point"/>, or null.  Nodes are tested in
        /// reverse order so the result matches what a renderer painting in order would show on top.
        /// </summary>
        public DeadlockNode? HitTest(LayoutPoint point)
        {
            for (var i = Nodes.Count - 1; i >= 0; i--)
            {
                if (Nodes[i].Bounds.Contains(point)) return Nodes[i];
            }
            return null;
        }
    }
}
