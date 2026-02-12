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
    /// Configuration for a Y-axis
    /// </summary>
    internal record YAxisConfiguration
    {
        /// <summary>
        /// Unique name for this axis (e.g., "MBsec", "IOPs", "Latency").
        /// Used to match with ColumnMetaData.AxisName.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Label text for the Y-axis
        /// </summary>
        public string Label { get; init; }

        /// <summary>
        /// Format string for Y-axis values (e.g., "0.0", "N0", "P2")
        /// </summary>
        public string Format { get; init; }

        /// <summary>
        /// Maximum value for the Y-axis. If not specified, auto-calculated.
        /// </summary>
        public double? MaxLimit { get; init; }

        /// <summary>
        /// Minimum value for the Y-axis. If not specified, defaults to 0.
        /// </summary>
        public double? MinLimit { get; init; }

        /// <summary>
        /// Position of the Y-axis. Default is Start (left).
        /// </summary>
        public AxisPosition Position { get; init; } = AxisPosition.Start;
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
        /// Whether line charts should have fill under the line. Default is false (no fill).
        /// Only applies to ChartTypes.Line.
        /// </summary>
        public bool LineFill { get; init; } = false;

        /// <summary>
        /// Optional dictionary to override series names for metric columns.
        /// Key = column name, Value = display name.
        /// If not specified, column names will be converted to friendly names automatically.
        /// </summary>
        public Dictionary<string, string> SeriesNames { get; init; }

        /// <summary>
        /// Configuration for multiple Y-axes. When specified, overrides YAxisLabel, YAxisFormat, YAxisMin, and YAxisMax.
        /// Each axis configuration should have a unique Name that matches ColumnMetaData.AxisName.
        /// The order of axes determines their rendering order and visual positioning.
        /// </summary>
        public YAxisConfiguration[] YAxes { get; init; }

        /// <summary>
        /// Maps metric column names to Y-axis indices (0-based).
        /// Only used when YAxes is specified and MetricColumns is used.
        /// Key = column name, Value = axis index (corresponding to YAxes array index).
        /// If a column is not in the map, it defaults to axis 0.
        /// OBSOLETE: This is auto-generated from ColumnMetaData.AxisName when using name-based axis mapping.
        /// </summary>
        public Dictionary<string, int> MetricColumnAxisMap { get; init; }

        /// <summary>
        /// Maps column names to their axis names for name-based axis assignment.
        /// Key = column name, Value = axis name (matching YAxisConfiguration.Name).
        /// When specified, this is used instead of MetricColumnAxisMap for more intuitive axis assignment.
        /// Typically populated from ColumnMetaData.AxisName.
        /// </summary>
        public Dictionary<string, string> ColumnAxisNames { get; init; }
    }
}