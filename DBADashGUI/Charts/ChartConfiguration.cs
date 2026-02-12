using LiveChartsCore.Measure;
using System;
using System.Collections.Generic;

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
        public const double DefaultLineSmoothness = 0.2;
        public const double DefaultGeometrySize = 8;
        // Required parameters

        public required string DateColumn { get; init; }

        /// <summary>
        /// Single metric column name. Use this when data has one metric column and optional SeriesColumn for grouping.
        /// Mutually exclusive with MetricColumns.
        /// </summary>
        public string MetricColumn { get; init; }

        /// <summary>
        /// Multiple metric column names. Each column will become a separate series.
        /// Use this when you want to chart multiple columns (e.g., "SizeGB", "UsedGB") as different series.
        /// Mutually exclusive with MetricColumn and SeriesColumn.
        /// </summary>
        public string[] MetricColumns { get; init; }

        // Optional parameters
        /// <summary>
        /// Column name to group data into separate series. If null, all data will be displayed as a single series.
        /// Only valid when using MetricColumn (not MetricColumns).
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

        /// <summary>
        /// Line smoothness for line charts (0 = sharp corners, 1 = maximum smoothing). Default is 0 (no smoothing).
        /// Only applies to ChartTypes.Line.
        /// </summary>
        public double LineSmoothness { get; init; } = DefaultLineSmoothness;

        /// <summary>
        /// Size of the geometry (points) on line/area charts. 0 hides points. Default is 0.
        /// </summary>
        public double GeometrySize { get; init; } = DefaultGeometrySize;

        /// <summary>
        /// Optional dictionary to override series names for metric columns.
        /// Key = column name, Value = display name.
        /// If not specified, column names will be converted to friendly names automatically.
        /// </summary>
        public Dictionary<string, string> SeriesNames { get; init; }
    }
}