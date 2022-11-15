using DBADashGUI.Performance;
using System.Collections.Generic;
using static DBADashGUI.Performance.IMetric;

namespace DBADashGUI
{

    /// <summary>
    /// Used by IOPerformance to store the state of the user control for serialization
    /// </summary>
    public class IOMetric : IMetric
    {
        public MetricTypes MetricType => MetricTypes.IO;

        public List<string> VisibleMetrics { get; set; } = new();

        public string Drive { get; set; }

        public IMetricChart GetChart()
        {
            return new IOPerformance() { Metric = this };
        }
    }
}
