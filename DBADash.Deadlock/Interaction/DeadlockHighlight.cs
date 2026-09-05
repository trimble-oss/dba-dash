using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.Deadlock.Layout;

namespace DBADash.Deadlock.Interaction
{
    /// <summary>
    /// How a node or edge relates to the node the reader has selected.  Higher values are stronger:
    /// where two relationships apply to the same node, the stronger one is the one that shows.
    /// </summary>
    public enum DeadlockHighlightRole
    {
        /// <summary>Not connected to the focus - drawn faded once anything is selected.</summary>
        None = 0,

        /// <summary>
        /// Another task of the same session as the focused process.  A parallel query contributes one
        /// process entry per thread, and knowing which boxes are really the same SPID is most of
        /// reading a parallel deadlock.
        /// </summary>
        Session = 1,

        /// <summary>
        /// A wait relationship with the focus: a resource the focused process is blocked on, or a
        /// process blocked on the focused resource.
        /// </summary>
        Wants = 2,

        /// <summary>
        /// An ownership relationship with the focus: a resource the focused process holds, or a
        /// process holding the focused resource.  Deliberately outranks <see cref="Wants"/> - who is
        /// holding what is the question a deadlock graph is usually being read to answer.
        /// </summary>
        Owns = 3,

        /// <summary>The selected node itself.</summary>
        Focus = 4
    }

    /// <summary>
    /// What is connected to the selected node, worked out once per selection so the renderer does not
    /// walk the edge list per frame.
    ///
    /// A large graph - a parallel query against one hot page can produce dozens of nodes and a
    /// hundred edges - is unreadable as a whole.  Selecting a node and having everything it holds,
    /// everything it is waiting for, and nothing else stand out is what makes it readable.
    /// </summary>
    public sealed class DeadlockHighlightMap
    {
        /// <summary>The map for "nothing selected": every role is <see cref="DeadlockHighlightRole.None"/>.</summary>
        public static readonly DeadlockHighlightMap Empty = new();

        private readonly Dictionary<DeadlockNode, DeadlockHighlightRole> _nodeRoles = new();
        private readonly Dictionary<DeadlockEdge, DeadlockHighlightRole> _edgeRoles = new();
        private readonly List<DeadlockNode> _ownershipRelated = new();
        private readonly List<DeadlockNode> _waitRelated = new();

        private DeadlockHighlightMap()
        {
        }

        /// <summary>The selected node, or null when nothing is selected.</summary>
        public DeadlockNode? Focus { get; private init; }

        /// <summary>
        /// True when a node is selected and there is therefore something to emphasise.  While false
        /// the renderer draws the graph plainly, with nothing faded.
        /// </summary>
        public bool IsActive => Focus is not null;

        /// <summary>
        /// The nodes on the ownership side of the focus: resources the focused process holds, or the
        /// processes holding the focused resource.
        /// </summary>
        public IReadOnlyList<DeadlockNode> OwnershipRelated => _ownershipRelated;

        /// <summary>
        /// The nodes on the wait side of the focus: resources the focused process is blocked on, or
        /// the processes blocked on the focused resource.  A node can appear here and in
        /// <see cref="OwnershipRelated"/> - a process holding a page in one mode while waiting to
        /// convert it is exactly the situation that deadlocks - and it is worth saying so, even
        /// though only the stronger role is drawn.
        /// </summary>
        public IReadOnlyList<DeadlockNode> WaitRelated => _waitRelated;

        public DeadlockHighlightRole RoleOf(DeadlockNode? node) =>
            node is not null && _nodeRoles.TryGetValue(node, out var role) ? role : DeadlockHighlightRole.None;

        public DeadlockHighlightRole RoleOf(DeadlockEdge? edge) =>
            edge is not null && _edgeRoles.TryGetValue(edge, out var role) ? role : DeadlockHighlightRole.None;

        /// <summary>
        /// Build the map for a selection.  Returns <see cref="Empty"/> when nothing is selected, or
        /// when the node does not belong to this layout.
        /// </summary>
        public static DeadlockHighlightMap For(DeadlockLayout layout, DeadlockNode? focus)
        {
            ArgumentNullException.ThrowIfNull(layout);

            if (focus is null || !layout.Nodes.Contains(focus)) return Empty;

            var map = new DeadlockHighlightMap { Focus = focus };
            map._nodeRoles[focus] = DeadlockHighlightRole.Focus;

            foreach (var edge in layout.Edges)
            {
                // Owner edges run resource -> process and waiter edges process -> resource, so the
                // far end of an edge touching the focus is always the node on the other side of the
                // relationship, whichever kind of node was selected.
                DeadlockNode other;
                if (ReferenceEquals(edge.From, focus)) other = edge.To;
                else if (ReferenceEquals(edge.To, focus)) other = edge.From;
                else continue;

                var role = edge.Kind == DeadlockEdgeKind.Owner
                    ? DeadlockHighlightRole.Owns
                    : DeadlockHighlightRole.Wants;

                map._edgeRoles[edge] = role;
                map.Relate(other, role);
            }

            map.AddSessionSiblings(layout, focus);
            return map;
        }

        /// <summary>
        /// The other tasks of a parallel query.  Marked below <see cref="DeadlockHighlightRole.Owns"/>
        /// and <see cref="DeadlockHighlightRole.Wants"/> so they read as context rather than as
        /// something the focus is waiting on, but kept out of the fade: a SPID split over eight
        /// threads is one participant, and hiding seven eighths of it helps nobody.
        /// </summary>
        private void AddSessionSiblings(DeadlockLayout layout, DeadlockNode focus)
        {
            if (focus is not DeadlockProcessNode { Process.Spid: { } spid }) return;

            foreach (var node in layout.Nodes)
            {
                if (ReferenceEquals(node, focus)) continue;
                if (node is not DeadlockProcessNode sibling || sibling.Process.Spid != spid) continue;

                Promote(node, DeadlockHighlightRole.Session);
            }
        }

        private void Relate(DeadlockNode node, DeadlockHighlightRole role)
        {
            var list = role == DeadlockHighlightRole.Owns ? _ownershipRelated : _waitRelated;
            if (!list.Contains(node)) list.Add(node);

            Promote(node, role);
        }

        /// <summary>Keeps the strongest role a node has earned - see <see cref="DeadlockHighlightRole"/>.</summary>
        private void Promote(DeadlockNode node, DeadlockHighlightRole role)
        {
            if (_nodeRoles.TryGetValue(node, out var existing) && existing >= role) return;

            _nodeRoles[node] = role;
        }
    }
}
