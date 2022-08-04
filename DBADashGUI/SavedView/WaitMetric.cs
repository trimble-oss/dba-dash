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
    /// Used by Waits to store the state of the user control for serialization
    /// </summary>
    public class WaitMetric : IMetric
    {
        public MetricTypes MetricType => MetricTypes.Waits;

        public string WaitType { get; set; }
        public bool CriticalWaitsOnly { get; set; }

        public IMetricChart GetChart()
        {
            return new Waits() { Metric = this };
        }
    }
}
