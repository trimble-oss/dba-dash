using DBADashGUI.Performance;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Lightweight wrapper used for storing chart configuration along with the table index
    /// in custom report metadata. This now also supports referencing runtime metric chart
    /// controls (IMetricChart) by type name so custom reports can include metric-type charts
    /// in addition to the existing chart configuration-based controls.
    /// </summary>
    public record CustomReportChart
    {
        /// <summary>
        /// Chart configuration for Cartesian/Pie charts. If present this is used to build
        /// a LiveCharts control from a DataTable.
        /// Assigning a non-null Config will clear Metric to maintain mutual exclusivity.
        /// </summary>
        public Charts.ChartConfigurationBase Config
        {
            get => field;
            set
            {
                field = value;
                if (value != null)
                {
                    Metric = null;
                }
            }
        }

        /// <summary>
        /// Index of the DataTable in the report DataSet to use as the data source for
        /// configuration-based charts.
        /// </summary>
        public int TableIndex { get; init; }

        /// <summary>
        /// Optional title to use when presenting a metric chart in a panel (used when
        /// Config is null).
        /// </summary>
        public string Title { get; init; }

        /// <summary>
        /// Persisted state for the metric control. This is the `IMetric` POCO
        /// instance. When present it will be assigned to the control's `Metric`
        /// property so the control's setter can update UI. Assigning a non-null
        /// Metric will clear Config to maintain mutual exclusivity.
        /// </summary>
        public IMetric Metric
        {
            get => field;
            set
            {
                field = value;
                if (value != null)
                {
                    Config = null;
                }
            }
        }
    }
}