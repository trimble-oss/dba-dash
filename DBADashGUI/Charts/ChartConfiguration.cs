using LiveChartsCore.Measure;
using System;

namespace DBADashGUI.Charts
{
    /// <summary>
    /// Types of charts that can be created
    /// </summary>
    public enum ChartTypes
    {
        /// <summary>
        /// Stacked area chart - areas stacked on top of each other
        /// </summary>
        StackedArea,

        /// <summary>
        /// Stacked column chart - columns stacked vertically
        /// </summary>
        StackedColumn,

        /// <summary>
        /// Line chart - individual lines for each series
        /// </summary>
        Line
    }

    /// <summary>
    /// Configuration for creating charts from DataTable
    /// </summary>
    internal record ChartConfiguration
    {
        // Required parameters

        public required string DateColumn { get; init; }
        public required string MetricColumn { get; init; }

        // Optional parameters
        /// <summary>
        /// Column name to group data into separate series. If null, all data will be displayed as a single series.
        /// </summary>
        public string SeriesColumn { get; init; }

        /// <summary>
        /// Label text for the Y-axis
        /// </summary>
        public string YAxisLabel { get; init; }

        /// <summary>
        /// Format string for Y-axis values (e.g., "N0", "P2")
        /// </summary>
        public string YAxisFormat { get; init; }

        /// <summary>
        /// Maximum value for the Y-axis. If not specified, auto-calculated.
        /// </summary>
        public double? YAxisMax { get; init; }

        /// <summary>
        /// Minimum value for the Y-axis. If not specified, defaults to 0.
        /// </summary>
        public double? YAxisMin { get; init; }

        /// <summary>
        /// Gets the minimum value of the X-axis, represented as a date and time.
        /// </summary>
        public DateTime? XAxisMin { get; init; }

        /// <summary>
        /// Gets the maximum value of the X-axis, represented as a date and time.
        /// </summary>
        public DateTime? XAxisMax { get; init; }

        /// <summary>
        /// Type of chart to create. Default is StackedColumn.
        /// </summary>
        public ChartTypes ChartType { get; init; } = ChartTypes.StackedColumn;

        /// <summary>
        /// Whether to show the legend. Default is true.
        /// </summary>
        public bool ShowLegend { get; init; } = true;

        /// <summary>
        /// Position of the legend. Default is Bottom.
        /// </summary>
        public LegendPosition LegendPosition { get; init; } = LegendPosition.Bottom;
    }
}