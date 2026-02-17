using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// User controls all implement IMetricChart which has a IMetric Metric property.  When serialized a class that implements IMetric should contain all the data needed to re-create the state of the chart.
    /// </summary>
    public interface IMetric
    {
        public enum MetricTypes
        {
            PerformanceCounter,
            CPU,
            IO,
            Blocking,
            ObjectExecution,
            Waits,
            ResourceGovernorWorkloadGroups,
            ResourceGovernorResourcePools
        }

        public enum AggregateTypes
        {
            Avg,
            Total,
            Sum,
            Max,
            Min,
            SampleCount,
            None
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public MetricTypes MetricType { get; }

        /// <summary>
        /// Create the associated chart control
        /// </summary>
        public IMetricChart GetChart();
    }
}