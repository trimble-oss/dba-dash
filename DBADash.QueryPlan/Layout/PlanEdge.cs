using System.Collections.Generic;

namespace DBADash.QueryPlan.Layout
{
    /// <summary>
    /// What an arrow's thickness measures.
    ///
    /// Rows is the default because it is what everyone reads a plan for.  Data size is the other
    /// half of the story: a million narrow rows and a thousand rows carrying a wide LOB column can
    /// cost the same to move, and only one of them looks heavy when drawn by row count.  Memory
    /// grants, spills and network transfer are all sized by bytes rather than rows.
    /// </summary>
    public enum PlanEdgeWidthMetric
    {
        /// <summary>Rows carried.</summary>
        Rows,

        /// <summary>Rows carried multiplied by the estimated row size.</summary>
        DataSize
    }

    /// <summary>
    /// Whether arrows are drawn by what the query did or by what the optimiser expected.
    ///
    /// Actual is the default, because on an actual plan it is what happened.  Estimated is there to
    /// switch to: flicking between the two is the quickest way to see where the estimates went wrong,
    /// since those are the arrows that change width.
    /// </summary>
    public enum PlanEdgeWidthBasis
    {
        /// <summary>
        /// Actual rows where the plan measured them.  An estimated plan has none, and is drawn by its
        /// estimates instead - see <see cref="PlanLayout.EffectiveEdgeWidthBasis"/>.
        /// </summary>
        Actual,

        /// <summary>
        /// The optimiser's estimate across all the executions it expected - see
        /// <see cref="Model.PlanOperator.EstimatedTotalRows"/>.
        /// </summary>
        Estimated,

        /// <summary>
        /// Actual rows, with the estimate drawn over them as a dashed outline coloured by how far
        /// out it was - see <see cref="PlanEdge.EstimateThickness"/>.  An underestimate is a fat
        /// pipe with the outline inside it; an overestimate is a thin pipe inside a hollow sleeve.
        /// Falls back to the estimates on a plan with no actuals, like <see cref="Actual"/>.
        /// </summary>
        Both
    }

    /// <summary>
    /// How close an arrow's estimate came to what actually flowed along it, as the success, warning
    /// and critical colours everything else in DBA Dash uses.
    /// </summary>
    public enum PlanEstimateAccuracy
    {
        /// <summary>Within ten times either way.</summary>
        Good,

        /// <summary>
        /// Ten times out or more - the threshold the estimate mismatch badge uses, so a warning
        /// coloured arrow and a badged operator mean the same thing.
        /// </summary>
        Warning,

        /// <summary>A hundred times out or more.</summary>
        Critical
    }

    /// <summary>
    /// A routed arrow between two nodes, carrying rows from an input into the operator that consumes
    /// them.
    ///
    /// <see cref="From"/> is always the producer and <see cref="To"/> the consumer, which is the
    /// opposite of the left-to-right reading order of the picture: the root is drawn on the left and
    /// the arrows point left, into it.  Naming them by their role rather than their position is what
    /// stops that being a constant source of confusion.
    /// </summary>
    public sealed class PlanEdge
    {
        /// <summary>The node producing the rows - drawn to the right.</summary>
        public PlanNode From { get; internal set; } = null!;

        /// <summary>The node consuming them - drawn to the left.</summary>
        public PlanNode To { get; internal set; } = null!;

        /// <summary>
        /// The polyline the arrow follows, from the producer's left edge to the consumer's right
        /// edge.  Three segments: out of the producer, across to the consumer's column, and in.
        /// </summary>
        public IReadOnlyList<LayoutPoint> Points { get; internal set; } = [];

        /// <summary>
        /// How thick to draw it, scaled from the row count - see
        /// <see cref="PlanLayoutOptions.MaxEdgeThickness"/> for why this carries so much weight.
        /// </summary>
        public double Thickness { get; internal set; }

        /// <summary>
        /// Rows carried, by the layout's <see cref="PlanLayout.EffectiveEdgeWidthBasis"/>: actual
        /// where the plan measured them, or the estimate for all the expected executions.
        /// </summary>
        public double Rows { get; internal set; }

        /// <summary>
        /// Bytes carried: <see cref="Rows"/> times the producer's estimated row size.  Zero when
        /// the plan gave no row size.
        /// </summary>
        public double DataSize { get; internal set; }

        /// <summary>
        /// The figure the thickness was scaled from, as text - a row count or a size, whichever the
        /// layout was asked to draw by, so the number on the arrow and its width always agree.
        /// </summary>
        public string Label { get; internal set; } = string.Empty;

        /// <summary>
        /// Where the label sits - just clear of the producer, where there is always room, rather
        /// than at the midpoint where several arrows converge on one consumer.
        /// </summary>
        public LayoutRect LabelBounds { get; internal set; }

        /// <summary>
        /// True when the rows are measured rather than estimated, so the renderer can distinguish
        /// what happened from what was expected.  False on an actual plan drawn by its estimates.
        /// </summary>
        public bool IsActual { get; internal set; }

        // ---------------------------------------------------------------- both figures, always
        //
        // Whatever the arrow is drawn by, it knows both figures, so hovering it can show them side
        // by side without the label having to grow.

        /// <summary>
        /// Rows the producer actually handed on, or null on an estimated plan.  Taken from the
        /// producer's input where SQL Server left the producer unmeasured - see
        /// <see cref="Model.PlanOperator.ActualRows"/> and <see cref="IsActualRowsInferred"/>.
        /// </summary>
        public double? ActualRows { get; internal set; }

        /// <summary>
        /// True when <see cref="ActualRows"/> is the producer's input's count - a Compute Scalar
        /// SQL Server did not measure - so the tooltip can say it was not measured directly.
        /// </summary>
        public bool IsActualRowsInferred { get; internal set; }

        /// <summary>
        /// Rows the optimiser expected, over all the executions it expected - see
        /// <see cref="Model.PlanOperator.EstimatedTotalRows"/>.
        /// </summary>
        public double EstimatedRows { get; internal set; }

        /// <summary>The producer's estimated row size in bytes, or null when the plan gave none.</summary>
        public double? RowSize { get; internal set; }

        /// <summary><see cref="ActualRows"/> in bytes, or null when either figure is missing.</summary>
        public double? ActualDataSize => ActualRows * RowSize;

        /// <summary><see cref="EstimatedRows"/> in bytes, or null when there is no row size.</summary>
        public double? EstimatedDataSize => EstimatedRows * RowSize;

        /// <summary>
        /// How far the estimate was out, as a multiple of at least one whichever way it went, so
        /// the two directions compare.  Null when nothing was measured.  Counts below one row are
        /// taken as one, so an estimate of 0.3 rows is not "infinitely" wrong about one.
        /// </summary>
        public double? EstimateError { get; internal set; }

        /// <summary><see cref="EstimateError"/> as a colour band, or null when nothing was measured.</summary>
        public PlanEstimateAccuracy? EstimateAccuracy { get; internal set; }

        /// <summary>
        /// The width the estimate is outlined at, drawn over the actual arrow - only when the arrows
        /// are drawn by <see cref="PlanEdgeWidthBasis.Both"/> and this one was measured.  On the same
        /// scale as <see cref="Thickness"/>, so the two widths compare directly.
        /// </summary>
        public double? EstimateThickness { get; internal set; }

        public override string ToString() => From.Title + " -> " + To.Title + " (" + Label + ")";
    }
}
