using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Layout
{
    /// <summary>
    /// A statement's plan with everything positioned: nodes with bounds, arrows routed between them,
    /// and the overall extent.  Produced by <see cref="PlanLayoutEngine"/> and consumed by the
    /// renderer and the interaction layer.
    ///
    /// Coordinates are normalised so a laid out plan starts at the origin, which saves every
    /// renderer from compensating for negative coordinates.
    /// </summary>
    public sealed class PlanLayout
    {
        /// <summary>
        /// The operators whose inputs are hidden, by <see cref="PlanNode.Id"/>.
        ///
        /// Held here rather than on the nodes because collapsing re-runs the layout, which builds
        /// new nodes - state on the old ones would be thrown away by the very action that needs it.
        /// </summary>
        internal HashSet<string> CollapsedIds { get; } = new(StringComparer.Ordinal);

        /// <summary>The engine that produced this layout, kept so it can be laid out again.</summary>
        internal PlanLayoutEngine? Engine { get; set; }

        public PlanStatement Statement { get; internal set; } = null!;

        /// <summary>
        /// The synthetic node at the head of the plan, which is what the arrows ultimately point
        /// into.  Always present, even for a statement with no plan, so the picture is never empty.
        /// </summary>
        public PlanNode Root { get; internal set; } = null!;

        /// <summary>
        /// Every visible node, parents before children.  Painted in this order, and hit tested in
        /// reverse, so what the reader clicks is what they can see.
        /// </summary>
        public IReadOnlyList<PlanNode> Nodes { get; internal set; } = [];

        public IReadOnlyList<PlanEdge> Edges { get; internal set; } = [];

        /// <summary>The extent of the layout including the configured margin.</summary>
        public LayoutRect Bounds { get; internal set; }

        /// <summary>The worst value of each metric, for scaling the bars - see
        /// <see cref="PlanLayoutMetrics"/>.</summary>
        public PlanLayoutMetrics Metrics { get; internal set; } = null!;

        /// <summary>True when any node is collapsed, so a host can offer to expand everything.</summary>
        public bool HasCollapsedNodes => CollapsedIds.Count > 0;

        /// <summary>What the arrow thickness currently measures.</summary>
        public PlanEdgeWidthMetric EdgeWidthMetric { get; internal set; }

        /// <summary>
        /// Draw the arrows by a different measure.  Returns false when that is already the measure.
        ///
        /// Only the arrows are routed again.  Thickness moves nothing else - the nodes keep their
        /// places and stay the same objects - so a selection, a search and the collapsed set all
        /// survive the switch, which a full re-layout would have thrown away.
        /// </summary>
        public bool SetEdgeWidthMetric(PlanEdgeWidthMetric metric)
        {
            if (EdgeWidthMetric == metric) return false;

            if (Engine is null)
            {
                throw new InvalidOperationException(
                    "This layout was not produced by a PlanLayoutEngine, so its arrows cannot be routed again.");
            }

            EdgeWidthMetric = metric;
            Edges = Engine.RouteEdges(this);
            return true;
        }

        /// <summary>
        /// Whether arrows are asked to be drawn by actual or estimated rows.  An estimated plan has no
        /// actuals to draw by, so what is actually drawn is <see cref="EffectiveEdgeWidthBasis"/>.
        /// </summary>
        public PlanEdgeWidthBasis EdgeWidthBasis { get; internal set; }

        /// <summary>
        /// What the arrows are drawn by: <see cref="EdgeWidthBasis"/>, unless that needs actual rows
        /// and the plan measured none.  Kept apart from the choice so that choosing actual still
        /// means actual on the next plan that has them.
        /// </summary>
        public PlanEdgeWidthBasis EffectiveEdgeWidthBasis =>
            EdgeWidthBasis == PlanEdgeWidthBasis.Estimated || !Metrics.HasRuntime
                ? PlanEdgeWidthBasis.Estimated
                : EdgeWidthBasis;

        /// <summary>
        /// Draw the arrows by actual or estimated rows.  Returns false when that is already the
        /// choice.  Like <see cref="SetEdgeWidthMetric"/>, only the arrows are routed again.
        /// </summary>
        public bool SetEdgeWidthBasis(PlanEdgeWidthBasis basis)
        {
            if (EdgeWidthBasis == basis) return false;

            if (Engine is null)
            {
                throw new InvalidOperationException(
                    "This layout was not produced by a PlanLayoutEngine, so its arrows cannot be routed again.");
            }

            EdgeWidthBasis = basis;
            Edges = Engine.RouteEdges(this);
            return true;
        }

        /// <summary>Which operator times the nodes show - see <see cref="Model.OperatorTimeMode"/>.</summary>
        public OperatorTimeMode OperatorTimeMode { get; internal set; }

        /// <summary>
        /// Show the other kind of operator time.  Returns false when that is already the kind shown.
        ///
        /// The plan is laid out again rather than only repainted, because the times are text on the
        /// nodes and a longer figure can widen a node.  Like collapsing, that builds new node objects.
        /// </summary>
        public bool SetOperatorTimeMode(OperatorTimeMode mode)
        {
            if (OperatorTimeMode == mode) return false;

            OperatorTimeMode = mode;
            Rebuild();
            return true;
        }

        /// <summary>
        /// Lay the plan out again, after the options of the engine that produced it have changed -
        /// the node widths, say.  Keeps the collapsed set; like collapsing, it builds new node objects.
        /// </summary>
        public void Relayout() => Rebuild();

        /// <summary>
        /// Hide or show a node's inputs, laying the plan out again.
        ///
        /// A big plan is read a stage at a time, and the alternative to collapsing - scrolling
        /// around forty nodes at a zoom where none of them is legible - is how a plan picture stops
        /// being useful.  Returns false when the node has no inputs to hide.
        /// </summary>
        public bool ToggleCollapse(PlanNode node)
        {
            ArgumentNullException.ThrowIfNull(node);

            // A collapsed node has no children in the layout, so "can this be collapsed" has to be
            // asked of the plan rather than of the picture.
            var hasInputs = node.Operator?.Children.Count > 0;
            if (!hasInputs) return false;

            if (!CollapsedIds.Remove(node.Id)) CollapsedIds.Add(node.Id);

            Rebuild();
            return true;
        }

        /// <summary>Show every hidden input.  Returns false when nothing was collapsed.</summary>
        public bool ExpandAll()
        {
            if (CollapsedIds.Count == 0) return false;

            CollapsedIds.Clear();
            Rebuild();
            return true;
        }

        /// <summary>
        /// The topmost node containing <paramref name="point"/>, or null.  Tested in reverse paint
        /// order so the result matches what is drawn on top.
        /// </summary>
        public PlanNode? HitTest(LayoutPoint point)
        {
            for (var i = Nodes.Count - 1; i >= 0; i--)
            {
                if (Nodes[i].Bounds.Contains(point)) return Nodes[i];
            }

            return null;
        }

        /// <summary>
        /// The arrow under <paramref name="point"/>, or null.  An arrow is as wide as it is drawn -
        /// the wider of its body and its estimate outline - plus <paramref name="tolerance"/>, so a
        /// hairline arrow can still be pointed at.
        ///
        /// Tested in reverse of the drawing order, which is thickest first, so where a thin arrow
        /// crosses a thick one the thin one on top is the one found.  The corners are tested as the
        /// square polyline the rounded corners are cut from, which is close enough to point at.
        /// </summary>
        public PlanEdge? EdgeAt(LayoutPoint point, double tolerance = 0)
        {
            for (var i = Edges.Count - 1; i >= 0; i--)
            {
                var edge = Edges[i];
                var reach = (Math.Max(edge.Thickness, edge.EstimateThickness ?? 0) / 2) + tolerance;

                for (var p = 1; p < edge.Points.Count; p++)
                {
                    if (DistanceToSegment(point, edge.Points[p - 1], edge.Points[p]) <= reach) return edge;
                }
            }

            return null;
        }

        private static double DistanceToSegment(LayoutPoint point, LayoutPoint start, LayoutPoint end)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var lengthSquared = (dx * dx) + (dy * dy);

            // How far along the segment the nearest point is, clamped to its ends.
            var along = lengthSquared == 0
                ? 0
                : Math.Clamp((((point.X - start.X) * dx) + ((point.Y - start.Y) * dy)) / lengthSquared, 0, 1);

            var nearestX = start.X + (along * dx);
            var nearestY = start.Y + (along * dy);

            return Math.Sqrt(((point.X - nearestX) * (point.X - nearestX)) + ((point.Y - nearestY) * (point.Y - nearestY)));
        }

        /// <summary>
        /// The collapse control under <paramref name="point"/>, or null.  Separate from
        /// <see cref="HitTest"/> because the control sits inside a node and has to win the click.
        /// </summary>
        public PlanNode? CollapseToggleAt(LayoutPoint point)
        {
            for (var i = Nodes.Count - 1; i >= 0; i--)
            {
                if (Nodes[i].CollapseToggleBounds is { } bounds && bounds.Contains(point)) return Nodes[i];
            }

            return null;
        }

        /// <summary>
        /// The node for an operator, or null when it is hidden under a collapsed parent.  Lets
        /// something else in the viewer - a warnings list, a search result - point at the picture.
        /// </summary>
        public PlanNode? FindNode(PlanOperator? node) =>
            node is null ? null : Nodes.FirstOrDefault(n => ReferenceEquals(n.Operator, node));

        public PlanNode? FindNodeById(string id) =>
            Nodes.FirstOrDefault(n => string.Equals(n.Id, id, StringComparison.Ordinal));

        /// <summary>
        /// The node an operator is represented by, following collapsed parents up until a visible
        /// node is found.  Selecting a hidden operator has to land somewhere, and the collapsed
        /// ancestor that is standing in for it is the only honest answer.
        /// </summary>
        public PlanNode? FindVisibleNode(PlanOperator? node)
        {
            for (var current = node; current is not null; current = current.Parent)
            {
                if (FindNode(current) is { } found) return found;
            }

            return Root;
        }

        private void Rebuild()
        {
            if (Engine is null)
            {
                throw new InvalidOperationException(
                    "This layout was not produced by a PlanLayoutEngine, so it cannot be laid out again.");
            }

            Engine.Build(this);
        }
    }
}
