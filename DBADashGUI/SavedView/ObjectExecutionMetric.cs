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
    /// Used by ObjectExecution to store the state of the user control for serialization
    /// </summary>
    public class ObjectExecutionMetric : IMetric
    {
        public MetricTypes MetricType => MetricTypes.ObjectExecution;

        public string Measure { get; set; } = "TotalDuration";

        public IMetricChart GetChart()
        {
            return new ObjectExecution() { Metric = this };
        }
    }
}
