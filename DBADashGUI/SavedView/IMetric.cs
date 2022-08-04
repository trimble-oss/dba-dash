using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DBADashGUI.Performance.IMetric;

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
            Waits
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

        public MetricTypes MetricType { get;}

        
        /// <summary>
        /// Create the associated chart control
        /// </summary>
        public IMetricChart GetChart();

    }

}

