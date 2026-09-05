namespace DBADash.Deadlock.Skia
{
    /// <summary>
    /// Fonts, line weights and thresholds for <see cref="DeadlockRenderer"/>.  Colours live
    /// separately in <see cref="DeadlockPalette"/> so a host can swap theme without touching metrics.
    /// </summary>
    public sealed class DeadlockRenderStyle
    {
        public string FontFamily { get; set; } = "Segoe UI";

        public float TitleFontSize { get; set; } = 12.5f;

        public float DetailFontSize { get; set; } = 10.5f;

        public float EdgeLabelFontSize { get; set; } = 9.5f;

        // The tooltip is drawn in screen space rather than under the view transform, so these are
        // absolute sizes and do not grow when the graph is zoomed in.  They are set a little larger
        // than the node text because the tooltip is where the detail is actually read.
        public float TooltipTitleFontSize { get; set; } = 18f;

        public float TooltipFontSize { get; set; } = 15f;

        public float NodeCornerRadius { get; set; } = 6f;

        public float NodeBorderWidth { get; set; } = 1.5f;

        /// <summary>Border weight for the selected node, and for the hover outline.</summary>
        public float EmphasisBorderWidth { get; set; } = 3f;

        public float EdgeWidth { get; set; } = 1.5f;

        /// <summary>Edges forming the deadlock cycle are drawn heavier than incidental ones.</summary>
        public float CycleEdgeWidth { get; set; } = 2.5f;

        /// <summary>
        /// Weight for the edges joining the selected node to what it holds.  Heavier than
        /// <see cref="WaitEdgeWidth"/>: ownership is the side of a deadlock worth following first, so
        /// it carries the emphasis where both are on screen together.
        /// </summary>
        public float OwnerEdgeWidth { get; set; } = 4f;

        /// <summary>Weight for the edges joining the selected node to what it is waiting for.</summary>
        public float WaitEdgeWidth { get; set; } = 3f;

        /// <summary>
        /// How far everything unrelated to the selected node is faded towards the background, from 0
        /// (no fade) to 1 (invisible).  Fading rather than hiding keeps the shape of the graph - which
        /// is the thing that says how big the pile-up is - while taking it out of the way of the part
        /// being read.
        /// </summary>
        public float UnrelatedFade { get; set; } = 0.78f;

        public float ArrowLength { get; set; } = 12f;

        public float ArrowWidth { get; set; } = 8f;

        /// <summary>
        /// Outline drawn around an edge label in that edge's own colour, so a label near a crossing
        /// still reads as belonging to one arrow rather than either.
        /// </summary>
        public float EdgeLabelBorderWidth { get; set; } = 1f;

        /// <summary>
        /// How far a label may sit from its own line before a leader is drawn back to it.  A label
        /// that had to move aside for another one would otherwise be anyone's guess.
        /// </summary>
        public float EdgeLabelLeaderDistance { get; set; } = 6f;

        /// <summary>
        /// Below this zoom the detail lines are dropped.  They are unreadable when shrunk and only
        /// add visual noise; the title alone still identifies the node.
        /// </summary>
        public double MinZoomForDetailLines { get; set; } = 0.5;

        /// <summary>Below this zoom edge labels are dropped, for the same reason.</summary>
        public double MinZoomForEdgeLabels { get; set; } = 0.7;

        public float TooltipPadding { get; set; } = 10f;

        public float TooltipCornerRadius { get; set; } = 4f;

        /// <summary>
        /// Widened to match the larger tooltip text, so a wait resource or statement still gets a
        /// useful amount of room before it is elided.
        /// </summary>
        public float TooltipMaxWidth { get; set; } = 560f;

        public float TooltipRowSpacing { get; set; } = 4f;

        /// <summary>Gap between the hovered node and its tooltip.</summary>
        public float TooltipOffset { get; set; } = 12f;
    }
}
