using DBADashGUI.Charts;
using DBADashGUI.Performance;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace DBADashGUI
{
    /// <summary>
    /// Used by ResourceGovernorResourcePools to store the state of the user control for serialization
    /// </summary>
    public class ResourceGovernorResourcePoolMetric : IMetric
    {
        public static readonly List<string> DefaultMetrics = ["cpu_percent", "cpu_cap_utilization_percent", "read_mb_per_sec"];

        public IMetric.MetricTypes MetricType => IMetric.MetricTypes.ResourceGovernorResourcePools;

        public bool ShowTable { get; set; } = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public ChartTypes ChartType { get; set; } = ChartTypes.StackedArea;

        public List<string> MetricsToDisplay { get; set; } = [.. DefaultMetrics];

        public Dictionary<string, float> RowPercentages { get; set; } = new();

        public IMetricChart GetChart()
        {
            return new ResourceGovernorResourcePools() { Metric = this };
        }
    }
}