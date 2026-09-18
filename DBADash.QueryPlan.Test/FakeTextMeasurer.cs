using DBADash.QueryPlan.Layout;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// A text measurer with no fonts in it, so layout tests have exact expected numbers.
    ///
    /// Real font metrics vary between machines and between Windows versions, which would make every
    /// assertion about a node's size a guess with a tolerance.  This is the seam
    /// <see cref="IPlanTextMeasurer"/> exists for.
    /// </summary>
    internal sealed class FakeTextMeasurer : IPlanTextMeasurer
    {
        /// <summary>Width per character, by role.  Titles are the widest, as a bold font is.</summary>
        public double TitleCharWidth { get; init; } = 8;

        public double DetailCharWidth { get; init; } = 6;

        public double MetricCharWidth { get; init; } = 5;

        public double EdgeLabelCharWidth { get; init; } = 5;

        public double TitleHeight { get; init; } = 16;

        public double DetailHeight { get; init; } = 14;

        public double MetricHeight { get; init; } = 12;

        public double EdgeLabelHeight { get; init; } = 12;

        public LayoutSize Measure(string text, PlanTextRole role)
        {
            var length = text?.Length ?? 0;

            return role switch
            {
                PlanTextRole.Title => new LayoutSize(length * TitleCharWidth, TitleHeight),
                PlanTextRole.Detail => new LayoutSize(length * DetailCharWidth, DetailHeight),
                PlanTextRole.Metric => new LayoutSize(length * MetricCharWidth, MetricHeight),
                _ => new LayoutSize(length * EdgeLabelCharWidth, EdgeLabelHeight)
            };
        }
    }
}
