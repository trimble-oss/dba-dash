using System;
using System.Collections.Generic;
using System.Data;

namespace DBADashGUI.Charts
{
    /// <summary>
    /// Series drawn over a cartesian chart from a different DataTable than the chart's own - CPU as a line over
    /// a stacked column chart of the spinlocks that might be burning it, say.
    ///
    /// <para>From its own table because what's overlaid is usually at a different grain to the chart.  The
    /// spinlock chart has a row per spinlock per bucket; CPU has one per bucket.  Carrying CPU in the chart's
    /// own table would mean repeating it on every row, and losing it for any bucket where the chart has no
    /// rows.</para>
    ///
    /// <para>Each metric column becomes a series, as with <see cref="ChartConfigurationBase.MetricColumns"/>.
    /// The X values have to be of the same kind as the chart's - dates on a date axis, numbers on a numeric
    /// one.  Category axes aren't supported, as the overlay's categories would have to be merged with the
    /// chart's.  An overlay that doesn't fit the chart is left off rather than failing it.</para>
    /// </summary>
    public record ChartOverlay
    {
        /// <summary>
        /// The X column in the overlay's table.  Null for a column of the same name as the chart's XColumn.
        /// </summary>
        public string XColumn { get; init; }

        /// <summary>
        /// Columns of the overlay's table to draw, each as its own series.
        /// </summary>
        public string[] MetricColumns { get; init; }

        /// <summary>
        /// How the overlay is drawn - Line or Scatter.  Stacked and column types aren't allowed: an overlay
        /// is drawn over the chart, and would otherwise stack onto or sit between the chart's own columns.
        /// </summary>
        public ChartTypes ChartType { get; init; } = ChartTypes.Line;

        /// <summary>
        /// An axis of the overlay's own, added after the chart's.  Usually wanted, as an overlay is rarely in
        /// the chart's units - set Position to End to put it on the right.  Null draws the overlay against
        /// the chart's first Y axis.
        /// </summary>
        public YAxisConfiguration YAxis { get; init; }

        /// <summary>
        /// Display names for the series.  Key = metric column.  Columns not named here get a friendly
        /// version of the column name, as on the chart itself.
        /// </summary>
        public Dictionary<string, string> SeriesNames { get; init; }

        /// <summary>
        /// Colour/marker overrides.  Key = metric column.  Worth setting - the theme palette gives series
        /// colours in order, so an overlay drawn after a chart with many series can get a colour already
        /// used by one of them.
        /// </summary>
        public Dictionary<string, ChartSeriesStyle> SeriesStyles { get; init; }

        /// <summary>
        /// Line smoothness (0 to 1).  Null for <see cref="ChartConfiguration.DefaultLineSmoothness"/>.
        /// </summary>
        public double? LineSmoothness { get; init; }

        /// <summary>
        /// Size of the points.  0 hides them, which suits a line over columns.
        /// </summary>
        public double GeometrySize { get; init; } = ChartConfiguration.DefaultGeometrySize;

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> where the overlay can't be drawn on any chart.
        /// Whether it fits a particular chart and table is checked when it's drawn.
        /// </summary>
        public void Validate()
        {
            var errors = new List<string>();
            if (MetricColumns == null || MetricColumns.Length == 0)
            {
                errors.Add("MetricColumns must be specified.");
            }
            if (ChartType is not (ChartTypes.Line or ChartTypes.Scatter))
            {
                errors.Add($"ChartType {ChartType} isn't supported for an overlay - use Line or Scatter.");
            }
            if (LineSmoothness is < 0 or > 1)
            {
                errors.Add("LineSmoothness must be between 0 and 1.");
            }
            if (GeometrySize < 0)
            {
                errors.Add("GeometrySize must be >= 0.");
            }
            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Invalid ChartOverlay: " + string.Join(" ", errors));
            }
        }
    }

    /// <summary>
    /// An overlay with the table it's drawn from, as passed to <see cref="ChartHelper"/>.
    /// </summary>
    internal sealed record ChartOverlayData(ChartOverlay Overlay, DataTable Table);
}
