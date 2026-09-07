using DBADashGUI.Performance;
using System.Collections.Generic;

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
        /// Optional drill-down opened when a point on this chart is clicked.  The parameters are mapped
        /// from the columns of the result set the point was drawn from, exactly as a grid drill-down maps
        /// them from the clicked row, so a chart shown without its grid is still a way into the detail.
        /// Configuration-based charts only - a metric chart handles its own clicks.
        /// </summary>
        public BaseDrillDownLinkColumnInfo DrillDown { get; init; }

        /// <summary>
        /// Draw this chart's X axis over the report's date range rather than over the range its data happens to
        /// cover.  The bounds are the @FromDate and @ToDate the report was actually run with, so a drill-down
        /// that narrows them narrows the axis too.
        ///
        /// <para>Set here rather than as XAxisMin and XAxisMax on the configuration because a report's charts
        /// are defined once, before there is a date range to put in them.</para>
        ///
        /// <para>Worth setting on a chart of events - deadlocks, failures - where the reader is judging how
        /// often something happened against the period they chose.  Without it, three deadlocks in an hour are
        /// drawn across an axis that spans only those three, which reads as steady trouble rather than as
        /// three deadlocks, and the last one always sits at the right hand edge whatever time it happened.</para>
        ///
        /// <para>Cartesian charts with a date X axis only.</para>
        /// </summary>
        public bool BindXAxisToDateRange { get; init; }

        /// <summary>
        /// Values that stand for a group of rows rather than one of them - the "Other" slice a pie rolls
        /// its tail into, or the "(none)" a report puts where a value was null.  Clicking them does
        /// nothing, because there is no filter that would reproduce what the slice counted.
        /// Matched against the values the drill-down maps.
        /// </summary>
        public List<string> DrillDownExcludedValues { get; init; }

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