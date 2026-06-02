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

        public IMetricChart GetChart()
        {
            return new ObjectExecution() { Metric = this };
        }
    }
}
