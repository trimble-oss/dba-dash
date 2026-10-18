#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.PerformanceAnalysis
{
    /// <summary>The parts of a performance analysis, in the order they are sent.</summary>
    public enum PerformanceSection
    {
        Context,
        CPU,
        Waits,
        IO,
        Metrics,
        Memory,
        Blocking,
        ObjectExecution,
        Latches,
        Spinlocks
    }

    /// <summary>
    /// What the analysis is about.  The same data either way - what changes is which sections are sent
    /// by default, how deep they go, and what the model is asked to concentrate on.
    /// </summary>
    public enum PerformanceAnalysisFocus
    {
        General,

        /// <summary>
        /// Latch and spinlock contention.  These are rarely the headline in a general analysis - they sit
        /// under waits and CPU - so a reader who has already found contention gets a prompt aimed at it
        /// and the latch and spinlock detail at full depth, with CPU and waits kept as context.
        /// </summary>
        LatchesAndSpinlocks
    }

    /// <summary>How one section is sent.</summary>
    public sealed class PerformanceSectionOptions
    {
        public PerformanceSection Section { get; init; }

        public bool Include { get; set; }

        /// <summary>
        /// Rows named individually in the section's summary - wait types, latch classes, clerks, objects.
        /// Null where the section has no ranking (CPU, Context).
        /// </summary>
        public int? TopN { get; set; }

        /// <summary>
        /// Whether the section sends a series over the window as well as the summary.  Null where it has
        /// no series to send.  A series is what lets the model say "it started at 14:10 and stopped at
        /// 14:40"; it is also most of the size of a section.
        /// </summary>
        public bool? TimeSeries { get; set; }

        public PerformanceSectionOptions Clone() => (PerformanceSectionOptions)MemberwiseClone();
    }

    /// <summary>Everything the reader chose about what goes in a performance analysis.</summary>
    public sealed class PerformanceAnalysisOptions
    {
        public const int MaxTopN = 50;

        /// <summary>Series named individually in a time series; the rest are rolled into "Other".</summary>
        public const int SeriesTopN = 5;

        public PerformanceAnalysisFocus Focus { get; init; }

        /// <summary>Compare with the same window a week earlier - see <see cref="PerformanceAnalysisWindow.Baseline"/>.</summary>
        public bool IncludeBaseline { get; set; } = true;

        public IReadOnlyList<PerformanceSectionOptions> Sections { get; init; } = Array.Empty<PerformanceSectionOptions>();

        public PerformanceSectionOptions this[PerformanceSection section] => Sections.First(s => s.Section == section);

        public bool Includes(PerformanceSection section) => Sections.Any(s => s.Section == section && s.Include);

        /// <summary>
        /// The defaults for a focus.  A general analysis sends everything at modest depth, with series
        /// only where the shape over time usually matters; the latch and spinlock focus sends those two at
        /// depth with their series, keeps CPU and waits as context, and leaves out the rest.
        /// </summary>
        public static PerformanceAnalysisOptions Defaults(PerformanceAnalysisFocus focus)
        {
            var general = focus == PerformanceAnalysisFocus.General;

            return new PerformanceAnalysisOptions
            {
                Focus = focus,
                Sections =
                [
                    new() { Section = PerformanceSection.Context, Include = true },
                    new() { Section = PerformanceSection.CPU, Include = true, TimeSeries = true },
                    new() { Section = PerformanceSection.Waits, Include = true, TopN = general ? 15 : 10, TimeSeries = general },
                    new() { Section = PerformanceSection.IO, Include = general, TopN = 10, TimeSeries = general },
                    new() { Section = PerformanceSection.Metrics, Include = general, TopN = 40, TimeSeries = null },
                    new() { Section = PerformanceSection.Memory, Include = general, TopN = 10, TimeSeries = null },
                    new() { Section = PerformanceSection.Blocking, Include = general, TopN = 5, TimeSeries = null },
                    new() { Section = PerformanceSection.ObjectExecution, Include = general, TopN = 10, TimeSeries = null },
                    new() { Section = PerformanceSection.Latches, Include = true, TopN = general ? 5 : 20, TimeSeries = !general },
                    new() { Section = PerformanceSection.Spinlocks, Include = true, TopN = general ? 5 : 20, TimeSeries = !general }
                ]
            };
        }

        public PerformanceAnalysisOptions Clone() => new()
        {
            Focus = Focus,
            IncludeBaseline = IncludeBaseline,
            Sections = Sections.Select(s => s.Clone()).ToList()
        };

        public static string Title(PerformanceSection section) => section switch
        {
            PerformanceSection.Context => "Instance context",
            PerformanceSection.CPU => "CPU",
            PerformanceSection.Waits => "Waits",
            PerformanceSection.IO => "IO",
            PerformanceSection.Metrics => "Metrics (performance counters)",
            PerformanceSection.Memory => "Memory clerks",
            PerformanceSection.Blocking => "Blocking",
            PerformanceSection.ObjectExecution => "Object execution",
            PerformanceSection.Latches => "Latches",
            PerformanceSection.Spinlocks => "Spinlocks",
            _ => section.ToString()
        };
    }
}
