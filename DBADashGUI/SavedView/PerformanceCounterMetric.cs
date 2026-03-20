using DBADashGUI.Performance;
using LiveChartsCore.Measure;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using static DBADashGUI.Performance.IMetric;

namespace DBADashGUI
{
    /// <summary>
    /// Used by PerformanceCounter to store the state of the user control for serialization
    /// </summary>
    public class PerformanceCounterMetric : IMetric
    {
        // Backwards compatible single counter properties. These map to the first
        // entry in the multi-counter lists so existing code that uses
        // CounterID/CounterName continues to work.
        [JsonIgnore]
        public int CounterID
        {
            get => (Counters != null && Counters.Count > 0) ? Counters[0].CounterID : 0;
            set
            {
                if (Counters == null) Counters = new List<DBADashGUI.Performance.Counter>();
                if (Counters.Count == 0) Counters.Add(new DBADashGUI.Performance.Counter() { CounterID = value, CounterName = string.Empty });
                else Counters[0].CounterID = value;
            }
        }

        public string CounterName
        {
            get => (Counters != null && Counters.Count > 0) ? Counters[0].CounterName : string.Empty;
            set
            {
                if (Counters == null) Counters = new List<Counter>();
                if (Counters.Count == 0) Counters.Add(new Counter() { CounterID = 0, CounterName = value });
                else Counters[0].CounterName = value;
            }
        }

        public string Title { get; set; } = string.Empty;

        public string GetTitle() => string.IsNullOrEmpty(Title) && (Counters != null) ? string.Join(", ", Counters.Select(c => c.FullName).Distinct().Order()) : Title;

        // New multi-counter support using the existing Counter class for richer metadata
        public List<Counter> Counters { get; set; } = new List<Counter>();

        private AggregateTypes _aggregateType = AggregateTypes.Avg;

        public LegendPosition LegendPosition { get; set; } = LegendPosition.Hidden;

        /// <summary>
        /// Legacy global aggregate type. For backward compatibility this will
        /// propagate to the first entry in the Counters list so older saved
        /// views that only set AggregateType continue to work. The per-counter
        /// aggregation flags (on Counter) remain authoritative when present.
        /// </summary>
        public AggregateTypes AggregateType
        {
            get => _aggregateType;
            set
            {
                _aggregateType = value;
                // Propagate to first counter for backward compatibility
                if (Counters == null) Counters = new List<Counter>();
                if (Counters.Count == 0)
                {
                    Counters.Add(new Counter() { CounterID = 0, CounterName = CounterName });
                }

                var c = Counters[0];
                // Clear existing flags
                c.Avg = false;
                c.Max = false;
                c.Min = false;
                c.Total = false;
                c.SampleCount = false;
                c.Current = false;

                switch (value)
                {
                    case AggregateTypes.Avg:
                        c.Avg = true;
                        break;

                    case AggregateTypes.Max:
                        c.Max = true;
                        break;

                    case AggregateTypes.Min:
                        c.Min = true;
                        break;

                    case AggregateTypes.Total:
                    case AggregateTypes.Sum:
                        c.Total = true;
                        break;

                    case AggregateTypes.SampleCount:
                        c.SampleCount = true;
                        break;

                    case AggregateTypes.None:
                    default:
                        // leave all flags false
                        break;
                }
            }
        }

        public MetricTypes MetricType => MetricTypes.PerformanceCounter;

        public IMetricChart GetChart()
        {
            return new PerformanceCounters() { Metric = this };
        }
    }
}