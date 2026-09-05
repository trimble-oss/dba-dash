namespace DBADash.Deadlock.Layout
{
    /// <summary>
    /// Tuning for <see cref="DeadlockLayoutEngine"/>.  Values are in layout units; the defaults suit
    /// a renderer treating one unit as one pixel at 100% zoom.
    /// </summary>
    public sealed class DeadlockLayoutOptions
    {
        /// <summary>How the nodes are arranged.  See <see cref="DeadlockLayoutStyle"/>.</summary>
        public DeadlockLayoutStyle Style { get; set; } = DeadlockLayoutStyle.Ring;

        /// <summary>Space between a node's text and its border.</summary>
        public double NodePadding { get; set; } = 8;

        /// <summary>Vertical gap between text lines inside a node.</summary>
        public double LineSpacing { get; set; } = 2;

        /// <summary>Nodes are never narrower than this, so small nodes stay uniform.</summary>
        public double MinNodeWidth { get; set; } = 120;

        /// <summary>
        /// Nodes are never wider than this.  Long values (a three part object name, a wait resource)
        /// are the renderer's problem to elide - layout only guarantees the box.
        /// </summary>
        public double MaxNodeWidth { get; set; } = 320;

        /// <summary>
        /// Nodes carrying a statement preview may grow to this width so more of the SQL text fits
        /// before the renderer has to elide it.  Wider than <see cref="MaxNodeWidth"/> because the
        /// statement is usually the detail people are hunting for, but still capped so a long
        /// statement cannot stretch the ring without bound.
        /// </summary>
        public double MaxStatementNodeWidth { get; set; } = 520;

        /// <summary>Minimum clear space between adjacent nodes on the ring.</summary>
        public double NodeSpacing { get; set; } = 48;

        /// <summary>Blank space left around the whole graph.</summary>
        public double Margin { get; set; } = 24;

        /// <summary>
        /// How far apart two edges joining the same pair of nodes are drawn.  That happens when a
        /// process both owns and waits on one resource - a conversion deadlock - and without the
        /// separation the two arrows land on the same line and read as a single double headed one.
        /// </summary>
        public double ParallelEdgeSpacing { get; set; } = 16;

        /// <summary>
        /// Space left around an edge label when working out how far apart two nodes joined by several
        /// edges have to be.  Their labels are spread along the line rather than stacked, so the line
        /// has to be long enough to hold them all with this much air between them.
        /// </summary>
        public double EdgeLabelClearance { get; set; } = 20;

        /// <summary>
        /// How many detail lines a node shows.  Kept low so nodes stay readable; the full detail
        /// belongs in the viewer's grids rather than on the graph.
        /// </summary>
        public int MaxDetailLines { get; set; } = 4;
    }
}
