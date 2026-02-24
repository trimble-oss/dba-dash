using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADashGUI.Charts
{
    /// <summary>
    /// Configuration for creating charts from DataTable
    /// </summary>
    public record ChartConfiguration : ChartConfigurationBase
    {
        public const double DefaultLineSmoothness = 0.2;
        public const double DefaultGeometrySize = 8;

        // Required parameters
        /// <summary>
        /// The column to use for the X axis. Can be a date/time column, numeric column, or categorical string column.
        /// </summary>
        public required string XColumn { get; init; }

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
        /// Gets the label for the X-axis.
        /// </summary>

        public string XAxisLabel { get; init; }

        /// <summary>
        /// Explicitly specifies the date unit interval for the X-axis (e.g., TimeSpan.FromMinutes(5)).
        /// When not specified, the date unit will be automatically calculated based on the data.
        /// For StackedColumn charts, this determines the width of the bars.
        /// </summary>
        public TimeSpan? DateUnit { get; init; }

        // ChartType, legend and series naming live on the base class
        public override ChartTypes ChartType { get; init; } = ChartTypes.StackedColumn;

        /// <summary>
        /// Line smoothness for line charts (0 = sharp corners, 1 = maximum smoothing). Default is 0.2.
        /// Only applies to ChartTypes.Line.
        /// </summary>
        public double LineSmoothness { get; init; } = DefaultLineSmoothness;

        /// <summary>
        /// Size of the geometry (points) on line/area charts. 0 hides points. Default is 8.
        /// </summary>
        public double GeometrySize { get; init; } = DefaultGeometrySize;

        /// <summary>
        /// Whether line charts should have fill under the line. Default is false (no fill).
        /// Only applies to ChartTypes.Line.
        /// </summary>
        public bool LineFill { get; init; } = false;

        // Series names are on base

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

        // ChartTitle lives on base

        /// <summary>
        /// Validate the configuration and throw <see cref="InvalidOperationException"/> if invalid.
        /// Checks include mutual exclusivity of metric fields, SeriesColumn usage, YAxes mapping consistency
        /// and value bounds for visual settings.
        /// </summary>
        public override void Validate()
        {
            // Validate base rules
            base.Validate();

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(XColumn))
            {
                errors.Add("XColumn is required.");
            }

            bool hasMetricColumn = !string.IsNullOrWhiteSpace(MetricColumn);
            bool hasMetricColumns = MetricColumns != null && MetricColumns.Length > 0;

            if (!hasMetricColumn && !hasMetricColumns)
            {
                errors.Add("Either MetricColumn or MetricColumns must be specified.");
            }

            if ((MetricColumnAxisMap != null || ColumnAxisNames != null) && YAxes == null)
            {
                errors.Add("MetricColumnAxisMap or ColumnAxisNames require YAxes to be specified.");
            }

            if (MetricColumnAxisMap != null && YAxes != null)
            {
                foreach (var kvp in MetricColumnAxisMap)
                {
                    if (kvp.Value < 0 || kvp.Value >= YAxes.Length)
                    {
                        errors.Add($"MetricColumnAxisMap maps column '{kvp.Key}' to invalid axis index {kvp.Value}.");
                    }
                }
            }

            if (ColumnAxisNames != null && YAxes != null)
            {
                var axisNames = new HashSet<string>(YAxes.Select(a => a.Name), StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in ColumnAxisNames)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Value) || !axisNames.Contains(kvp.Value))
                    {
                        errors.Add($"ColumnAxisNames maps column '{kvp.Key}' to unknown axis name '{kvp.Value}'.");
                    }
                }
            }

            if (LineSmoothness < 0 || LineSmoothness > 1)
            {
                errors.Add("LineSmoothness must be between 0 and 1.");
            }

            if (GeometrySize < 0)
            {
                errors.Add("GeometrySize must be >= 0.");
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Invalid ChartConfiguration: " + string.Join(" ", errors));
            }
        }
    }
}