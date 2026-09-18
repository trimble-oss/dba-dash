namespace DBADash.QueryPlan.Skia
{
    /// <summary>
    /// Fonts, line weights and thresholds for <see cref="PlanRenderer"/>.  Colours live separately
    /// in <see cref="PlanPalette"/> so a host can swap theme without touching metrics.
    /// </summary>
    public sealed class PlanRenderStyle
    {
        public string FontFamily { get; set; } = "Segoe UI";

        public float TitleFontSize { get; set; } = 12.5f;

        public float DetailFontSize { get; set; } = 10.5f;

        public float MetricFontSize { get; set; } = 9.5f;

        public float EdgeLabelFontSize { get; set; } = 9.5f;

        // The tooltip is drawn in screen space rather than under the view transform, so these are
        // absolute sizes and do not grow when the plan is zoomed in.  They are set a little larger
        // than the node text because the tooltip is where the detail is actually read.
        public float TooltipTitleFontSize { get; set; } = 16f;

        public float TooltipFontSize { get; set; } = 13.5f;

        public float NodeCornerRadius { get; set; } = 8f;

        public float NodeBorderWidth { get; set; } = 1f;

        /// <summary>Border weight for the selected node, and for the hover outline.</summary>
        public float EmphasisBorderWidth { get; set; } = 2.5f;

        /// <summary>The corner radius of the chip the operator glyph is drawn on.</summary>
        public float IconCornerRadius { get; set; } = 6f;

        /// <summary>
        /// Line weight of the glyph symbol, in glyph units - the symbols are drawn on a 24 unit
        /// grid, so this is scaled with the chip rather than being an absolute width.
        /// </summary>
        public float IconStrokeWidth { get; set; } = 2f;

        /// <summary>Blank space between the chip edge and the symbol on it, in glyph units.</summary>
        public float IconSymbolInset { get; set; } = 3.5f;

        /// <summary>
        /// Arrows are drawn with this much of a lighter outline around them, so two arrows crossing
        /// read as one passing over the other rather than as a join.
        /// </summary>
        public float EdgeOutlineWidth { get; set; } = 1.5f;

        public float ArrowLength { get; set; } = 11f;

        /// <summary>
        /// The dashed outline an arrow's estimate is drawn with, in screen pixels at every zoom - see
        /// <see cref="PlanRenderer"/>'s estimate outline for why not layout units.
        /// </summary>
        public float EstimateOutlineWidth { get; set; } = 1.5f;

        /// <summary>Dash and gap of the estimate outline, in screen pixels.</summary>
        public float EstimateDashLength { get; set; } = 5f;

        public float EstimateDashGap { get; set; } = 3.5f;

        /// <summary>
        /// How much wider than the arrow body the head is.  Added rather than absolute, so a thin
        /// arrow still gets a visible head and a thick one does not get a spike.
        /// </summary>
        public float ArrowHeadSpread { get; set; } = 7f;

        // Badge size and spacing are not here: the node has to be measured wide enough for its
        // badges as well as its title, so they belong to the layout - see PlanLayoutOptions.

        /// <summary>
        /// The smallest the warning triangle is ever drawn on screen, in pixels.  Everything else
        /// shrinks with the zoom, but zoomed out to fit a big plan the glyph it sits on is a speck,
        /// and the triangles are how the reader finds the operators worth zooming in on.
        /// </summary>
        public float MinWarningMarkerScreenSize { get; set; } = 12f;

        /// <summary>
        /// Below this zoom the subtitle and metric lines are dropped.  They are unreadable when
        /// shrunk and only add visual noise; the title and the glyph still identify the operator.
        /// </summary>
        public double MinZoomForDetailLines { get; set; } = 0.55;

        /// <summary>Below this zoom the row counts on the arrows are dropped, for the same reason.</summary>
        public double MinZoomForEdgeLabels { get; set; } = 0.7;

        /// <summary>
        /// Below this zoom nodes are drawn as plain blocks with no text or glyph at all.  A plan of
        /// two hundred operators zoomed out to fit is a shape, not a diagram, and trying to draw
        /// text at two pixels high costs a great deal and shows nothing.
        /// </summary>
        public double MinZoomForNodeContent { get; set; } = 0.25;

        /// <summary>
        /// How far everything off the selected path is faded towards the background, from 0 (no
        /// fade) to 1 (invisible).  Fading rather than hiding keeps the shape of the plan - which is
        /// the thing that says how big it is - while taking it out of the way of the part being
        /// read.
        /// </summary>
        public float UnrelatedFade { get; set; } = 0.72f;

        /// <summary>
        /// Fade the rest of the plan when a node is selected.  Off by default: a plan is read as a
        /// whole far more often than one path through it is, so dimming on every click would be in
        /// the way.  The viewer turns it on for the Follow Data Path action.
        /// </summary>
        public bool FadeOffPath { get; set; }

        public float TooltipPadding { get; set; } = 10f;

        public float TooltipCornerRadius { get; set; } = 5f;

        public float TooltipMaxWidth { get; set; } = 520f;

        /// <summary>
        /// The narrowest an operator's description is wrapped to.  A tooltip of a few short figures
        /// would otherwise wrap its description a handful of words to a line, and a column that
        /// narrow is harder to read than a wider tooltip.
        /// </summary>
        public float TooltipDescriptionMinWidth { get; set; } = 360f;

        public float TooltipRowSpacing { get; set; } = 3f;

        /// <summary>
        /// The most lines a wrapping tooltip value takes - see <see cref="DBADash.QueryPlan.Interaction.PlanTooltipRow.Wraps"/>.
        /// Enough for a typical predicate in full; a generated one runs to pages, and the last line
        /// ends in an ellipsis with the whole of it in the properties panel.
        /// </summary>
        public int TooltipMaxWrappedLines { get; set; } = 8;

        /// <summary>The gap between a label and its value on a tooltip row.</summary>
        public float TooltipLabelGap { get; set; } = 14f;

        /// <summary>Gap between the hovered node and its tooltip.</summary>
        public float TooltipOffset { get; set; } = 14f;
    }
}
