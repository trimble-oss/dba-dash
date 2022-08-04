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
    /// Used by CPU to store the state of the user control for serialization
    /// </summary>
    public class CPUMetric : IMetric
    {
        public MetricTypes MetricType => MetricTypes.CPU;

        private AggregateTypes _aggregateType = AggregateTypes.Avg;
        public AggregateTypes AggregateType
        {
            get
            {
                return _aggregateType;
            }
            set
            {
                if (value == AggregateTypes.Avg || value == AggregateTypes.Max)
                {
                    _aggregateType = value;
                }
                else
                {
                    throw new Exception("Aggregate Type not supported for CPUMetric:" + Enum.GetName(value));
                }
            }
        }

        public IMetricChart GetChart()
        {
            return new CPU() { Metric = this };
        }
    }
}
