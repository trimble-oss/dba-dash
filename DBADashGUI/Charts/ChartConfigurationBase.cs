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
        /// Column chart - individual columns for each series
        /// </summary>
        Column,

        /// <summary>
        /// Line chart - individual lines for each series
        /// </summary>
        Line,

        /// <summary>
        /// Pie chart - category/value slices
        /// </summary>
        Pie
    }

    /// <summary>
    /// Base configuration for charts containing options common to all chart types.
    /// Cartesian-specific settings remain on `ChartConfiguration`.
    /// </summary>
    public abstract record ChartConfigurationBase
    {
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

        /// <summary>
        /// Column name to group data into separate series. If null, all data will be displayed as a single series.
        /// Only valid when using MetricColumn (not MetricColumns).
        /// </summary>
        public string SeriesColumn { get; init; }

        /// <summary>
        /// Type of chart to create. Default is StackedColumn.
        /// Made virtual so derived types (e.g., pie configuration) can override it
        /// and ensure correct value is round-tripped by serializers.
        /// </summary>
        public abstract ChartTypes ChartType { get; init; }

        /// <summary>
        /// Position of the legend. Default is Bottom.
        /// </summary>
        public LegendPosition LegendPosition { get; init; } = LegendPosition.Bottom;

        /// <summary>
        /// Optional dictionary to override series names for metric columns.
        /// Key = column name, Value = display name.
        /// If not specified, column names will be converted to friendly names automatically.
        /// </summary>
        public Dictionary<string, string> SeriesNames { get; init; }

        public string ChartTitle { get; init; }

        /// <summary>
        /// Validate the base configuration. Throws InvalidOperationException on errors.
        /// This performs checks that apply to all chart types (mutual exclusivity, SeriesColumn usage).
        /// More specific validations are performed by derived classes.
        /// </summary>
        public virtual void Validate()
        {
            var errors = new List<string>();

            bool hasMetricColumn = !string.IsNullOrWhiteSpace(MetricColumn);
            bool hasMetricColumns = MetricColumns != null && MetricColumns.Length > 0;

            if (hasMetricColumn && hasMetricColumns)
            {
                errors.Add("MetricColumn and MetricColumns are mutually exclusive.");
            }

            if (!hasMetricColumn && !string.IsNullOrWhiteSpace(SeriesColumn))
            {
                errors.Add("SeriesColumn is only valid when MetricColumn is used (single metric column with grouping).");
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Invalid ChartConfigurationBase: " + string.Join(" ", errors));
            }
        }
    }

    /// <summary>
    /// Configuration for a Y-axis
    /// </summary>
    public record YAxisConfiguration
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
}