using DBADashGUI.Charts;
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
        /// The type of chart to render. Defaults to Line. The field initializer ensures older saved
        /// views (which don't persist this property) continue to render as line charts rather than
        /// falling back to the enum's zero value.
        /// </summary>
        public ChartTypes ChartType { get; set; } = ChartTypes.Line;

        /// <summary>
        /// Whether line charts fill the area under the line. Only affects the Line chart type.
        /// Defaults to true to preserve the appearance of older saved views (which rendered filled lines).
        /// </summary>
        public bool LineFill { get; set; } = true;

        /// <summary>
        /// Whether data points (geometry) are drawn on line/area charts. Defaults to true.
        /// Points are auto-hidden on dense datasets regardless of this setting. Scatter charts
        /// always show points as that is all they render.
        /// </summary>
        public bool ShowPoints { get; set; } = true;

        /// <summary>
        /// Whether line/area charts use smoothed (curved) lines. Defaults to false (straight segments).
        /// </summary>
        public bool SmoothLines { get; set; } = false;

        /// <summary>
        /// Optional fixed minimum for the Y-axis. When null the axis auto-scales.
        /// </summary>
        public double? YAxisMin { get; set; }

        /// <summary>
        /// Optional fixed maximum for the Y-axis. When null the axis auto-scales.
        /// </summary>
        public double? YAxisMax { get; set; }

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