using DBADashGUI.Performance;
using static DBADashGUI.Performance.IMetric;

namespace DBADashGUI
{


    /// <summary>
    /// Used to store the state of the Blocking chart
    /// </summary>
    public class BlockingMetric : IMetric
    {
        public MetricTypes MetricType => MetricTypes.Blocking;

        public IMetricChart GetChart()
        {
            return new Blocking() { Metric = this };
        }
    }
}
