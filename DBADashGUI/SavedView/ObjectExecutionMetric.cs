using DBADashGUI.Performance;
using static DBADashGUI.Performance.IMetric;

namespace DBADashGUI
{

    /// <summary>
    /// Used by ObjectExecution to store the state of the user control for serialization
    /// </summary>
    public class ObjectExecutionMetric : IMetric
    {
        public MetricTypes MetricType => MetricTypes.ObjectExecution;

        public string Measure { get; set; } = "TotalDuration";

        public int TopRows { get; set; } = 5;

        public bool IncludeOther { get; set; } = true;

        public enum ExecutionStatsSources
        {
            /// <summary>Object execution stats - procedures, functions and triggers (dbo.ObjectExecutionStats_Get)</summary>
            Objects,

            /// <summary>Statement level query stats (dbo.QueryStatsCharts_Get)</summary>
            Queries
        }

        public ExecutionStatsSources Source { get; set; } = ExecutionStatsSources.Objects;

        /// <summary>The measure used when <see cref="Source"/> is Queries - a key of ObjectExecution's query measures.  Kept apart from <see cref="Measure"/> as the two sources measure different things.</summary>
        public string QueryMeasure { get; set; } = "CPU_PerSecond";

        public IMetricChart GetChart()
        {
            return new ObjectExecution() { Metric = this };
        }
    }
}
