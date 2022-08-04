using DBADashGUI.Performance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DBADashGUI.Performance.IMetric;

namespace DBADashGUI
{

    /// <summary>
    /// Used by PerformanceCounter to store the state of the user control for serialization
    /// </summary>
    public class PerformanceCounterMetric : IMetric
    {
        public int CounterID { get; set; }
        public string CounterName { get; set; }

        public AggregateTypes AggregateType { get; set; } = AggregateTypes.Avg;

        public MetricTypes MetricType => MetricTypes.PerformanceCounter;

        public IMetricChart GetChart()
        {
            return new PerformanceCounters() { Metric = this };
        }
    }
}
