using DBADash.QueryPlan.Layout;

namespace DBADash.QueryPlan.Interaction
{
    /// <summary>Limits and step sizes for <see cref="PlanViewController"/>.</summary>
    public sealed class PlanViewOptions
    {
        public double MinZoom { get; set; } = 0.1;

        public double MaxZoom { get; set; } = 8.0;

        /// <summary>Multiplier applied per zoom-in step; its reciprocal is used to zoom out.</summary>
        public double ZoomStep { get; set; } = 1.2;

        /// <summary>
        /// Upper bound on the scale zoom-to-fit may use, so a two operator plan fills the window
        /// without being blown up to absurdity.
        /// </summary>
        public double MaxFitZoom { get; set; } = 1.25;

        /// <summary>
        /// Lower bound on the scale the automatic fit may use - when a plan is opened, and as the
        /// window is resized.  Actual size by default: a plan zoomed out to fit a window it is ten
        /// times the size of is a picture nobody can read, so a plan that does not fit is shown at
        /// full size to be scrolled instead.
        ///
        /// Only the automatic fit is floored.  <see cref="PlanViewController.ZoomToFit"/> - the Fit
        /// button - always fits the whole plan, however far out that is.
        /// </summary>
        public double MinAutoFitZoom { get; set; } = 1.0;

        /// <summary>
        /// Re-fit when the viewport changes size, so resizing the window keeps the whole plan in
        /// view.  Only applies until the user zooms or pans themselves - after that the view is
        /// theirs and resizing leaves it alone, until they ask to fit again.
        /// </summary>
        public bool RefitOnViewportChange { get; set; } = true;

        /// <summary>Predicates on a tooltip are collapsed to one line and truncated to this length.</summary>
        public int MaxTooltipPredicateLength { get; set; } = PlanTooltipBuilder.DefaultMaxPredicateLength;

        /// <summary>
        /// Which metric the node bars and the heat colouring measure.  Cost is the default because
        /// it is the only one an estimated plan has.
        /// </summary>
        public PlanHeatMetric HeatMetric { get; set; } = PlanHeatMetric.OperatorCost;

        /// <summary>
        /// Say what each operator does on its tooltip.  On by default, for readers still learning the
        /// operators; off for those who know them and want the figures alone.
        /// </summary>
        public bool ShowOperatorDescriptions { get; set; } = true;
    }
}
