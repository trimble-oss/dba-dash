using DBADashGUI.Charts;
using DBADashGUI.Performance;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace DBADashGUI
{
    /// <summary>
    /// Used by ResourceGovernorWorkloadGroupsMetrics to store the state of the user control for serialization
    /// </summary>
    public class ResourceGovernorWorkloadGroupsMetric : IMetric
    {
        public static readonly List<string> DefaultMetrics = ["cpu_cores", "requests_per_min"];

        public IMetric.MetricTypes MetricType => IMetric.MetricTypes.ResourceGovernorWorkloadGroups;

        public bool ShowTable { get; set; } = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public ChartTypes ChartType { get; set; } = ChartTypes.StackedArea;

        public List<string> MetricsToDisplay { get; set; } = [.. DefaultMetrics];

        public IMetricChart GetChart()
        {
            return new ResourceGovernorWorkloadGroupsMetrics() { Metric = this };
        }
    }
}