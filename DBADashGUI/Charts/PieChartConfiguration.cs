using System;
using System.Collections.Generic;

namespace DBADashGUI.Charts
{
    /// <summary>
    /// Configuration specific to pie charts. Inherits common chart options but provides
    /// pie-specific fields such as category/value columns and donut sizing.
    /// </summary>
    public record PieChartConfiguration : ChartConfigurationBase
    {
        // Pie charts are always Pie. Expose a read-only getter and an init accessor
        // that only accepts Pie so the value cannot be changed to anything else.
        public override ChartTypes ChartType
        {
            get => ChartTypes.Pie;
            init
            {
                if (value != ChartTypes.Pie)
                {
                    throw new InvalidOperationException("ChartType for PieChartConfiguration must be ChartTypes.Pie");
                }
            }
        }
        /// <summary>
        /// Column containing category labels for slices. Mutually exclusive with MetricColumns.
        /// </summary>
        public string CategoryColumn { get; init; }

        /// <summary>
        /// Column containing numeric values for each category. Used with CategoryColumn.
        /// </summary>
        public string ValueColumn { get; init; }

        /// <summary>
        /// Inner radius for donut charts (0..1 where 1 would be fully hollow). Default 0.5 when IsDonut is true.
        /// </summary>
        public double? InnerRadius { get; init; }

        /// <summary>
        /// Whether hovering will push out the slice (if supported).
        /// </summary>
        public bool ExplodeOnHover { get; init; } = true;

        /// <summary>
        /// Minimum percent (0-100) of the total a slice must represent to avoid being grouped into the "Other" bucket.
        /// Set to 0 to disable grouping. Default is 1.
        /// </summary>
        public double MinSlicePercent { get; init; } = 1;

        /// <summary>
        /// Label to use for the grouped bucket when small slices are combined. Defaults to "Other".
        /// </summary>
        public string OtherLabel { get; init; } = "Other";

        public override void Validate()
        {
            // Validate base rules
            base.Validate();

            var errors = new List<string>();

            var hasMetricColumns = MetricColumns != null && MetricColumns.Length > 0;
            var hasCategoryAndValue = !string.IsNullOrWhiteSpace(CategoryColumn) && !string.IsNullOrWhiteSpace(ValueColumn);

            if (!hasMetricColumns && !hasCategoryAndValue)
            {
                errors.Add("Pie charts require either MetricColumns or CategoryColumn+ValueColumn.");
            }

            if (hasMetricColumns && hasCategoryAndValue)
            {
                errors.Add("MetricColumns and CategoryColumn+ValueColumn are mutually exclusive for pie charts.");
            }

            if (InnerRadius.HasValue && (InnerRadius < 0 || InnerRadius > 1))
            {
                errors.Add("InnerRadius must be between 0 and 1 (fraction of radius). When 1 the pie is fully hollow.");
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Invalid PieChartConfiguration: " + string.Join(" ", errors));
            }
        }
    }
}