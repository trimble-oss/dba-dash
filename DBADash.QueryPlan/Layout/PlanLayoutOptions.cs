namespace DBADash.QueryPlan.Layout
{
    /// <summary>
    /// Tuning for <see cref="PlanLayoutEngine"/>.  Values are in layout units; the defaults suit a
    /// renderer treating one unit as one pixel at 100% zoom.
    /// </summary>
    public sealed class PlanLayoutOptions
    {
        /// <summary>Space between a node's content and its border.</summary>
        public double NodePadding { get; set; } = 8;

        /// <summary>Vertical gap between the text lines inside a node.</summary>
        public double LineSpacing { get; set; } = 2;

        /// <summary>
        /// The square the operator glyph is drawn in, at the left of the node.  Large enough to be
        /// recognised at the zoom a whole plan fits at, which is the zoom most plans are read at.
        /// </summary>
        public double IconSize { get; set; } = 26;

        /// <summary>Gap between the glyph and the text beside it.</summary>
        public double IconSpacing { get; set; } = 8;

        /// <summary>
        /// Nodes are never narrower than this, so a plan of short operator names does not come out
        /// as a row of ragged little boxes.  Kept low: every pixel of it is paid again in every
        /// column, and on a deep plan that is the difference between fitting the screen and not.
        /// </summary>
        public double MinNodeWidth { get; set; } = 120;

        /// <summary>
        /// Nodes are never wider than this.  A long three part object name is the renderer's problem
        /// to elide - layout only guarantees the box - because one wide node would otherwise set the
        /// width of its whole column.
        /// </summary>
        public double MaxNodeWidth { get; set; } = 300;

        /// <summary>
        /// The glyph centred at the top of the node with the text centred beneath it, as SSMS draws
        /// operators, rather than beside it.  The text then has the node's whole width, so the node
        /// can be much narrower: a long operator name wraps onto a second line, and the figures split
        /// at their separators, rather than widening the node.
        /// </summary>
        public bool IconAboveText { get; set; }

        /// <summary>
        /// Wrap an object name too long for the widest node onto more lines, rather than cutting it
        /// short.  Off by default: the name is on the tooltip in full, and a node three lines taller
        /// for one long index name pushes the rest of the plan apart.  On, the whole name is on the
        /// picture - which is what a screenshot of a plan needs.
        /// </summary>
        public bool WrapObjectNames { get; set; }

        /// <summary>The most lines a wrapped object name takes - see <see cref="WrapObjectNames"/>.</summary>
        public int MaxObjectNameLines { get; set; } = 4;

        /// <summary>
        /// Put each operator's node id on it, ahead of its figures.
        ///
        /// Off by default, because it is a number the plan gives its operators rather than anything
        /// about the query, and every node pays a little width for it.  On, because everything that
        /// names an operator in words - a card saying a conversion happens on node 9, the warnings
        /// list, the properties panel - names it by that number, and finding node 9 in a picture that
        /// never shows one means clicking through the plan.
        /// </summary>
        public bool ShowNodeIds { get; set; }

        /// <summary>
        /// Set <see cref="MinNodeWidth"/> and <see cref="MaxNodeWidth"/> to a preset, and
        /// <see cref="IconAboveText"/> with it - only <see cref="PlanNodeWidth.Stacked"/> puts the icon above.
        /// </summary>
        public void SetNodeWidth(PlanNodeWidth width)
        {
            (MinNodeWidth, MaxNodeWidth) = PlanNodeWidths.Range(width);
            IconAboveText = width == PlanNodeWidth.Stacked;
        }

        /// <summary>
        /// Height of the bar showing the operator's share of the chosen metric, or zero to leave it
        /// out.  A bar reads faster than the percentage beside it and costs four pixels.
        /// </summary>
        public double MetricBarHeight { get; set; } = 4;

        /// <summary>
        /// The square one badge occupies on the top edge of a node.
        ///
        /// A layout concern rather than a rendering one, because the node has to be measured wide
        /// enough for its badges.  They straddle the top border rather than sitting on the title's
        /// line: sharing the line made the title's badges set the width of a third of the nodes in
        /// a typical plan, and on the border they cost no width at all.
        /// </summary>
        public double BadgeSize { get; set; } = 15;

        /// <summary>
        /// The width of the warning triangle over the corner of a node's glyph.  It is a little less
        /// tall than wide, which is the shape of the triangle.  The renderer never draws it smaller
        /// than a set size on screen, so at the zoom a whole plan fits at it is still there to find.
        /// </summary>
        public double WarningMarkerSize { get; set; } = 14;

        /// <summary>Gap between adjacent badges.</summary>
        public double BadgeSpacing { get; set; } = 3;

        /// <summary>
        /// Inset of the badge strip from the node's right edge.  Enough to clear the rounded corner,
        /// so the badges sit on the straight part of the border.
        /// </summary>
        public double BadgeInset { get; set; } = 10;

        /// <summary>
        /// Clear space between columns, which is where the arrows are routed.  Room for a row count
        /// label beside the producer and the corner of a thick arrow beside the consumer, and no
        /// more.
        /// </summary>
        public double ColumnSpacing { get; set; } = 48;

        /// <summary>Set <see cref="ColumnSpacing"/> to a preset.</summary>
        public void SetColumnSpacing(PlanColumnSpacing spacing) => ColumnSpacing = PlanColumnSpacings.Spacing(spacing);

        /// <summary>Minimum clear space between two nodes stacked in the same column.</summary>
        public double RowSpacing { get; set; } = 22;

        /// <summary>
        /// Which row each node is put on.  See <see cref="PlanVerticalLayout"/>.
        /// </summary>
        public PlanVerticalLayout VerticalLayout { get; set; } = PlanVerticalLayout.FirstChildAligned;

        /// <summary>Blank space left around the whole plan.</summary>
        public double Margin { get; set; } = 28;

        /// <summary>
        /// Every node in a column is given the width of the widest node in that column, so the
        /// arrows between two columns are all the same length and the plan reads as a sequence of
        /// stages rather than a ragged pile.  Turn it off to size each node to its own content.
        /// </summary>
        public bool UniformColumnWidths { get; set; } = true;

        /// <summary>The thinnest an arrow is drawn, for a row count of zero.</summary>
        public double MinEdgeThickness { get; set; } = 1.5;

        /// <summary>
        /// The thickest an arrow is drawn, for the busiest arrow in the statement - or for the
        /// floor, when the statement never gets that busy.  See <see cref="EdgeWidthRowsFloor"/>.
        ///
        /// Thickness is the one part of a plan picture that shows where the volume is, and a plan
        /// where one arrow carries a million rows and the rest carry ten is the common case - so the
        /// scale has to be wide enough for the difference to be unmissable, and capped so the fat
        /// arrow does not swamp the nodes it joins.
        /// </summary>
        public double MaxEdgeThickness { get; set; } = 22;

        /// <summary>What arrow thickness measures.  See <see cref="PlanEdgeWidthMetric"/>.</summary>
        public PlanEdgeWidthMetric EdgeWidthMetric { get; set; } = PlanEdgeWidthMetric.Rows;

        /// <summary>Actual or estimated arrows.  See <see cref="PlanEdgeWidthBasis"/>.</summary>
        public PlanEdgeWidthBasis EdgeWidthBasis { get; set; } = PlanEdgeWidthBasis.Actual;

        /// <summary>
        /// Whether the times on the nodes, and the time bars, are each operator's own or the figures
        /// as SQL Server reported them.  See <see cref="Model.OperatorTimeMode"/>.
        /// </summary>
        public Model.OperatorTimeMode OperatorTimeMode { get; set; } = Model.OperatorTimeMode.Own;

        /// <summary>
        /// The smallest row count the thickness scale tops out at.  The scale runs up to whichever is
        /// larger - this, or the busiest arrow in the plan.
        ///
        /// Without a floor every plan gets one full-width arrow, so a query moving ten rows looks as
        /// heavy as one moving ten million and the width stops meaning anything between plans.  With
        /// it, a small plan stays thin, and a plan past the floor still scales against its own
        /// busiest arrow, so the widths remain relative within it.  Zero turns the floor off.
        /// </summary>
        public double EdgeWidthRowsFloor { get; set; } = 1_000_000;

        /// <summary>
        /// The same floor as <see cref="EdgeWidthRowsFloor"/>, in bytes, for
        /// <see cref="PlanEdgeWidthMetric.DataSize"/>.  100 MB is a million rows at a typical 100
        /// byte row, so switching between the two metrics does not change what counts as a small
        /// plan.
        /// </summary>
        public double EdgeWidthDataSizeFloor { get; set; } = 100d * 1024 * 1024;

        /// <summary>
        /// How far from a node the arrow turns the corner, as a fraction of the column gap.  Half
        /// puts the corner midway between the two columns.
        /// </summary>
        public double EdgeCornerFraction { get; set; } = 0.5;

        /// <summary>
        /// The radius the arrow's corners are rounded by.  Square corners on a thick polyline read as
        /// a mistake; a small radius reads as a pipe.
        /// </summary>
        public double EdgeCornerRadius { get; set; } = 8;
    }
}
