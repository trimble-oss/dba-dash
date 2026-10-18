#nullable enable
using System;
using System.Collections.Generic;
using System.Data;

namespace DBADash.PerformanceAnalysis
{
    /// <summary>
    /// What the repository returned for a performance analysis, as the procedures behind the charts
    /// returned it.
    ///
    /// The same procedures as the charts rather than one of its own, deliberately: an answer that says
    /// WRITELOG averaged 40ms/sec while the Waits chart beside it says 25 leaves the reader trusting
    /// neither.  Reusing them also means the rules about rollups, coverage and rates live in one place.
    ///
    /// Times are UTC, as the procedures return them; <see cref="PerformanceAnalysisPayload"/> converts the
    /// ones it shows.  Every table is optional - a section whose table is missing says it has no data
    /// rather than failing the analysis.
    /// </summary>
    public sealed class PerformanceAnalysisData
    {
        public required PerformanceAnalysisWindow Window { get; init; }

        public string Instance { get; init; } = string.Empty;

        public int InstanceId { get; init; }

        /// <summary>dbo.InstanceInfo_Get: Name, Value.</summary>
        public DataTable? InstanceInfo { get; set; }

        /// <summary>dbo.Configuration_Get for the instance: name, value, default_value, IsDefault, configuration_id.</summary>
        public DataTable? Configuration { get; set; }

        /// <summary>dbo.CPU_Get in buckets: EventTime, SQLProcessCPU, OtherCPU, MaxCPU.</summary>
        public DataTable? Cpu { get; set; }

        public DataTable? CpuBaseline { get; set; }

        /// <summary>dbo.WaitsSummary_Get.</summary>
        public DataTable? Waits { get; set; }

        public DataTable? WaitsBaseline { get; set; }

        /// <summary>dbo.Waits_Get in buckets, top <see cref="PerformanceAnalysisOptions.SeriesTopN"/> wait types.</summary>
        public DataTable? WaitsSeries { get; set; }

        /// <summary>dbo.IOSummary_Get grouped by drive.</summary>
        public DataTable? IODrives { get; set; }

        public DataTable? IODrivesBaseline { get; set; }

        /// <summary>dbo.IOSummary_Get grouped by database.</summary>
        public DataTable? IODatabases { get; set; }

        /// <summary>dbo.IOStats_Get in buckets, for all drives.</summary>
        public DataTable? IOSeries { get; set; }

        /// <summary>dbo.PerformanceCounterSummary_Get for the instance.</summary>
        public DataTable? Metrics { get; set; }

        public DataTable? MetricsBaseline { get; set; }

        /// <summary>dbo.MemoryUsage_Get: the latest snapshot of each clerk with its min, avg and max over the window.</summary>
        public DataTable? Memory { get; set; }

        public DataTable? MemoryBaseline { get; set; }

        /// <summary>dbo.BlockingSnapshots_Get: the worst blocking snapshots, by blocked wait time.</summary>
        public DataTable? Blocking { get; set; }

        public DataTable? BlockingBaseline { get; set; }

        /// <summary>dbo.ObjectExecutionStatsSummary_Get, with the baseline as its compare period.</summary>
        public DataTable? ObjectExecution { get; set; }

        /// <summary>dbo.LatchStats_Get: the summary, then the series.  A single Message table when not collected.</summary>
        public DataSet? Latches { get; set; }

        public DataSet? LatchesBaseline { get; set; }

        /// <summary>dbo.SpinlockStats_Get: the summary, then the series.  A single Message table when not collected.</summary>
        public DataSet? Spinlocks { get; set; }

        public DataSet? SpinlocksBaseline { get; set; }

        /// <summary>The dbo.BlockingSnapshots_Get row limit - at it, the count is a floor rather than a count.</summary>
        public int BlockingSnapshotLimit { get; init; } = 500;

        /// <summary>Sections that failed to load, and why.  Said in the section rather than failing the whole analysis.</summary>
        public Dictionary<PerformanceSection, string> Errors { get; } = new();

        /// <summary>Things the reader should know about how the data was chosen, shown at the top of the payload.</summary>
        public List<string> Notes { get; } = new();
    }
}
