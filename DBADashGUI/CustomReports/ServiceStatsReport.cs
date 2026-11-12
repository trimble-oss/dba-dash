using DBADashGUI.Charts;
using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// The DBA Dash service's own resource usage over time - CPU, memory, threads and the collection work queue - against the
    /// number of SQL connections it's handling, so growth that follows the estate can be told apart from growth that doesn't.
    ///
    /// A line per service.  The services are the ones that collect or import the instances in context, so at the root it's
    /// every service and on a tag it's the services behind those instances.  The data is the service's, not an instance's:
    /// a service collecting a hundred instances is one line whichever of them is selected.
    ///
    /// Working Set per SQL Connection is the chart that answers "is it using more memory because it's doing more?" - it stays
    /// level when memory grows with the connections, and rises for a leak or a new cost.  The connection count includes the
    /// Azure DBs and read replicas the service finds for itself, which the config doesn't list.
    ///
    /// The grid is a row per service summarising the date range; the over time data only feeds the charts, so it's out of
    /// the grid but still selectable from the results.
    ///
    /// Every column has a description, shown as the grid's header tooltip - the columns are the service's internals, and
    /// several (busy workers, skipped, the thread pool queue) only mean something with an explanation.
    /// </summary>
    internal class ServiceStatsReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ProcedureName = "ServiceStats_Get",
            QualifiedProcedureName = "dbo.ServiceStats_Get",
            ReportVisibilityRole = "public",
            ReportName = "Service Stats",
            Description = "The DBA Dash service's own CPU, memory, threads and work queue over time, against the SQL connections it's handling.  Collected while the service has LogInternalPerformanceCounters on.",
            CanEditReport = false,
            ChartVisible = true,
            TableVisible = true,
            // Twelve charts in three rows: usage and what drives it, memory and the process, then the work queue
            ChartLayoutColumns = 4,
            Charts = new List<CustomReportChart>
            {
                // The service is usually near idle, so a floor on the axis keeps a percent or two of noise from filling the chart
                Line("CPU", "CpuPct", "% of host", "N1", minimumMax: 10),
                Line("Working Set", "WorkingSetMB", "MB", "N0"),
                Line("Working Set per SQL Connection", "WorkingSetPerSQLConnectionMB", "MB", "N2"),
                Line("SQL Connections", "SQLConnections", "Connections", "N0"),

                Line("Managed Heap", "ManagedHeapMB", "MB", "N0"),
                Line("Threads", "ThreadCount", "Threads", "N0"),
                Line("Handles", "HandleCount", "Handles", "N0"),
                Line("GC Pause", "GCPausePct", "% of time", "N1", minimumMax: 5),

                new()
                {
                    TableIndex = 2,
                    Title = "Work Queue by Priority",
                    BindXAxisToDateRange = true,
                    Config = new ChartConfiguration
                    {
                        ChartType = ChartTypes.StackedArea,
                        ChartTitle = "Work Queue by Priority",
                        XColumn = "SnapshotDate",
                        MetricColumn = "QueueLength",
                        SeriesColumn = "Series",
                        YAxisLabel = "Items",
                        YAxisFormat = "N0",
                        // Usually empty or close to it - a floor keeps a queue of one or two from looking like a backlog
                        YAxisMinimumMax = 10
                    }
                },
                Line("Work Queue Wait (avg)", "QueueWaitAvgMs", "ms", "N0", minimumMax: 1000),
                Line("Busy Workers (max)", "MaxBusyWorkers", "Workers", "N0", minimumMax: 10),
                Line("Skipped Collections", "QueueSkippedPct", "% of scheduled", "N1", minimumMax: 10)
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Summary",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["DBADashAgentID"] = new ColumnMetadata { Visible = false, Description = "The service's ID in dbo.DBADashAgent" },
                        ["Service"] = new ColumnMetadata { Description = "The host the service runs on, and the service name" },
                        ["AgentHostName"] = new ColumnMetadata { Visible = false, Description = "The host the service runs on" },
                        ["AgentServiceName"] = new ColumnMetadata { Visible = false, Description = "The service name" },
                        ["AgentVersion"] = new ColumnMetadata { Alias = "Version", Description = "The DBA Dash version the service is running - as it last reported it (dbo.DBADashAgent)" },
                        ["AgentPath"] = new ColumnMetadata { Alias = "Path", Description = "The folder the service runs from on its host" },
                        ["LastSample"] = new ColumnMetadata { Alias = "Last Sample", Description = "The service's latest sample in the date range.  Current values are as of this sample - an old one is a service that's stopped, or can't reach the repository.  Blank for a service with no samples in the range: they're only collected while it has LogInternalPerformanceCounters on." },
                        ["ProcessStartDate"] = new ColumnMetadata { Alias = "Process Start", Description = "When the service process started, as of the last sample" },
                        ["Restarts"] = new ColumnMetadata { Description = "Times the service restarted in the date range" },
                        ["AvgCpuPct"] = new ColumnMetadata { Alias = "Avg CPU %", FormatString = "N2", Description = "Share of the host's processors the service used over the date range.  100 is every core busy." },
                        ["MaxCpuPct"] = new ColumnMetadata { Alias = "Max CPU %", FormatString = "N2", Description = "CPU % for the busiest sample in the date range" },
                        ["ProcessorCount"] = new ColumnMetadata { Alias = "Processors", Description = "Logical processors on the host - what CPU % is a share of" },
                        ["WorkingSetMB"] = new ColumnMetadata { Alias = "Working Set MB", FormatString = "N1", Description = "Physical memory in use now (the last sample) - the Memory column in Task Manager" },
                        ["MinWorkingSetMB"] = new ColumnMetadata { Alias = "Min Working Set MB", FormatString = "N1", Description = "The lowest working set sampled in the date range" },
                        ["MaxWorkingSetMB"] = new ColumnMetadata { Alias = "Max Working Set MB", FormatString = "N1", Description = "The highest working set sampled in the date range" },
                        ["WorkingSetPerSQLConnectionMB"] = new ColumnMetadata { Alias = "Working Set per SQL Connection MB", FormatString = "N2", Description = "Working set over SQL connections, now.  Level when memory grows with the estate; rising for a leak or a new cost." },
                        ["MinWorkingSetPerSQLConnectionMB"] = new ColumnMetadata { Alias = "Min Working Set per SQL Connection MB", FormatString = "N2", Description = "The lowest working set per SQL connection sampled in the date range.  Well below the current value means memory has grown faster than the estate." },
                        ["MaxWorkingSetPerSQLConnectionMB"] = new ColumnMetadata { Alias = "Max Working Set per SQL Connection MB", FormatString = "N2", Description = "The highest working set per SQL connection sampled in the date range" },
                        ["PrivateBytesMB"] = new ColumnMetadata { Alias = "Private Bytes MB", FormatString = "N1", Description = "Memory committed to the process alone (Private Bytes, Commit size), now" },
                        ["ManagedHeapMB"] = new ColumnMetadata { Alias = "Managed Heap MB", FormatString = "N1", Description = "Allocated on the .NET heap, now.  Growing with the working set points at managed memory; a working set growing without it points elsewhere (native memory, the SQL client)." },
                        ["MaxManagedHeapMB"] = new ColumnMetadata { Alias = "Max Managed Heap MB", FormatString = "N1", Description = "The largest .NET heap sampled in the date range" },
                        ["Gen2CollectionsPerHour"] = new ColumnMetadata { Alias = "Gen 2 GCs /hr", FormatString = "N1", Description = "Generation 2 (full) garbage collections per hour of sampled time - expensive, so a steady rate of these is worth a look" },
                        ["GCPausePct"] = new ColumnMetadata { Alias = "GC Pause %", FormatString = "N2", Description = "Share of the sampled time the process was paused for garbage collection" },
                        ["ThreadCount"] = new ColumnMetadata { Alias = "Threads", Description = "Operating system threads in the process, now" },
                        ["MaxThreadCount"] = new ColumnMetadata { Alias = "Max Threads", Description = "The most threads sampled in the date range" },
                        ["MaxThreadPoolQueueLength"] = new ColumnMetadata { Alias = "Max Thread Pool Queue", Description = "The most work items sampled waiting for a thread pool thread.  A queue that stays up is thread pool starvation." },
                        ["HandleCount"] = new ColumnMetadata { Alias = "Handles", Description = "Operating system handles, now.  Steady growth is a leak." },
                        ["MaxHandleCount"] = new ColumnMetadata { Alias = "Max Handles", Description = "The most handles sampled in the date range" },
                        ["SourceConnections"] = new ColumnMetadata { Alias = "Sources", Description = "Sources the service is handling now - SQL connections plus folder and S3 imports" },
                        ["SQLConnections"] = new ColumnMetadata { Alias = "SQL Connections", Description = "SQL connections the service is handling now - an instance, Azure DB or read replica each, including those the service found itself" },
                        ["MinSQLConnections"] = new ColumnMetadata { Alias = "Min SQL Connections", Description = "The fewest SQL connections sampled in the date range" },
                        ["MaxSQLConnections"] = new ColumnMetadata { Alias = "Max SQL Connections", Description = "The most SQL connections sampled in the date range" },
                        ["DiscoveredConnections"] = new ColumnMetadata { Alias = "Discovered", Description = "Azure DBs found from a master connection, and read replicas, now - monitored, but not listed in the config" },
                        ["OfflineInstances"] = new ColumnMetadata { Alias = "Offline", Description = "Source connections the service is treating as offline, now" },
                        ["QueueLength"] = new ColumnMetadata { Alias = "Queue Length", Description = "Collections waiting in the work queue now, all priorities.  Blank without queue based scheduling." },
                        ["AvgQueueLength"] = new ColumnMetadata { Alias = "Avg Queue Length", FormatString = "N1", Description = "The average work queue length sampled in the date range" },
                        ["MaxQueueLength"] = new ColumnMetadata { Alias = "Max Queue Length", Description = "The longest work queue sampled in the date range" },
                        ["Workers"] = new ColumnMetadata { Description = "Workers taking collections off the work queue" },
                        ["MaxBusyWorkers"] = new ColumnMetadata { Alias = "Max Busy Workers", Description = "The most workers processing a collection at once in the date range.  Equal to Workers means none were free at some point." },
                        ["QueueItemsStartedPerMin"] = new ColumnMetadata { Alias = "Items Started /min", FormatString = "N1", Description = "Collections taken off the work queue by a worker, per minute - averaged over the time the service was sampled in the date range, so a period it was down doesn't lower it" },
                        ["QueueSkippedPct"] = new ColumnMetadata { Alias = "Skipped %", FormatString = "N2", Description = "Share of the date range's attempts to queue a collection that were skipped because the previous run of the same one was still queued or running - the schedule is more frequent than the service can keep up with.  Above 0 is worth a look." },
                        ["QueueWaitAvgMs"] = new ColumnMetadata { Alias = "Queue Wait Avg ms", Description = "Average time a collection waited in the queue for a worker in the date range" },
                        ["QueueWaitMaxMs"] = new ColumnMetadata { Alias = "Queue Wait Max ms", Description = "The longest any collection waited in the queue for a worker in the date range" },
                        ["SampledPct"] = new ColumnMetadata { Alias = "Sampled %", FormatString = "N1", Description = "Share of the date range the service's samples cover.  Less than 100 is a service that was stopped, or couldn't report, for some of it - the averages and rates cover only the sampled time." },
                        ["Samples"] = new ColumnMetadata { Description = "Samples in the date range - one a minute by default (ServiceStatsFrequencySeconds), while LogInternalPerformanceCounters is on" }
                    }
                },
                [1] = new CustomReportResult
                {
                    ResultName = "Over Time",
                    // Feeds the charts - the summary is the grid
                    ShowWithAllResults = false,
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["DBADashAgentID"] = new ColumnMetadata { Visible = false, Description = "The service's ID in dbo.DBADashAgent" },
                        ["Service"] = new ColumnMetadata { Description = "The host the service runs on, and the service name" },
                        ["AgentHostName"] = new ColumnMetadata { Visible = false, Description = "The host the service runs on" },
                        ["AgentServiceName"] = new ColumnMetadata { Visible = false, Description = "The service name" },
                        ["SnapshotDate"] = new ColumnMetadata { Alias = "Time", Description = "The start of the period.  The period is a single sample for a short date range, and longer as the range grows." },
                        ["CpuPct"] = new ColumnMetadata { Alias = "CPU %", FormatString = "N2", Description = "Share of the host's processors the service used over the period.  100 is every core busy." },
                        ["MaxCpuPct"] = new ColumnMetadata { Alias = "Max CPU %", FormatString = "N2", Description = "CPU % for the busiest sample in the period" },
                        ["ProcessorCount"] = new ColumnMetadata { Alias = "Processors", Description = "Logical processors on the host - what CPU % is a share of" },
                        ["WorkingSetMB"] = new ColumnMetadata { Alias = "Working Set MB", FormatString = "N1", Description = "Physical memory in use at the end of the period - the Memory column in Task Manager" },
                        ["MaxWorkingSetMB"] = new ColumnMetadata { Alias = "Max Working Set MB", FormatString = "N1", Description = "The highest working set sampled in the period" },
                        ["PeakWorkingSetMB"] = new ColumnMetadata { Alias = "Peak Working Set MB", FormatString = "N1", Description = "The highest working set since the process started, including between samples" },
                        ["PrivateBytesMB"] = new ColumnMetadata { Alias = "Private Bytes MB", FormatString = "N1", Description = "Memory committed to the process alone (Private Bytes, Commit size) at the end of the period" },
                        ["ManagedHeapMB"] = new ColumnMetadata { Alias = "Managed Heap MB", FormatString = "N1", Description = "Allocated on the .NET heap.  Growing with the working set points at managed memory; a working set growing without it points elsewhere (native memory, the SQL client)." },
                        ["GCCommittedMB"] = new ColumnMetadata { Alias = "GC Committed MB", FormatString = "N1", Description = "Memory the .NET garbage collector has committed, as of its last collection" },
                        ["Gen0CollectionsPerHour"] = new ColumnMetadata { Alias = "Gen 0 GCs /hr", FormatString = "N1", Description = "Generation 0 garbage collections per hour - frequent and cheap, they follow the allocation rate" },
                        ["Gen1CollectionsPerHour"] = new ColumnMetadata { Alias = "Gen 1 GCs /hr", FormatString = "N1", Description = "Generation 1 garbage collections per hour" },
                        ["Gen2CollectionsPerHour"] = new ColumnMetadata { Alias = "Gen 2 GCs /hr", FormatString = "N1", Description = "Generation 2 (full) garbage collections per hour - expensive, so a steady rate of these is worth a look" },
                        ["GCPausePct"] = new ColumnMetadata { Alias = "GC Pause %", FormatString = "N2", Description = "Share of the period the process was paused for garbage collection" },
                        ["ThreadCount"] = new ColumnMetadata { Alias = "Threads", Description = "Operating system threads in the process at the end of the period" },
                        ["ThreadPoolThreads"] = new ColumnMetadata { Alias = "Thread Pool Threads", Description = ".NET thread pool threads at the end of the period" },
                        ["MaxThreadPoolQueueLength"] = new ColumnMetadata { Alias = "Max Thread Pool Queue", Description = "The most work items sampled waiting for a thread pool thread.  A queue that stays up is thread pool starvation." },
                        ["HandleCount"] = new ColumnMetadata { Alias = "Handles", Description = "Operating system handles at the end of the period.  Steady growth is a leak." },
                        ["SourceConnections"] = new ColumnMetadata { Alias = "Sources", Description = "Sources the service is handling - SQL connections plus folder and S3 imports" },
                        ["SQLConnections"] = new ColumnMetadata { Alias = "SQL Connections", Description = "SQL connections the service is handling - an instance, Azure DB or read replica each, including those the service found itself" },
                        ["DiscoveredConnections"] = new ColumnMetadata { Alias = "Discovered", Description = "Azure DBs found from a master connection, and read replicas - monitored, but not listed in the config" },
                        ["WorkingSetPerSQLConnectionMB"] = new ColumnMetadata { Alias = "Working Set per SQL Connection MB", FormatString = "N2", Description = "Working set over SQL connections.  Level when memory grows with the estate; rising for a leak or a new cost." },
                        ["OfflineInstances"] = new ColumnMetadata { Alias = "Offline", Description = "Source connections the service is treating as offline at the end of the period" },
                        ["QueueLength"] = new ColumnMetadata { Alias = "Queue Length", Description = "Collections waiting in the work queue at the end of the period, all priorities.  Blank without queue based scheduling." },
                        ["MaxQueueLength"] = new ColumnMetadata { Alias = "Max Queue Length", Description = "The longest work queue sampled in the period, all priorities" },
                        ["QueueLengthHigh"] = new ColumnMetadata { Alias = "Queue High", Description = "Collections waiting at high priority at the end of the period - taken before normal and low" },
                        ["QueueLengthNormal"] = new ColumnMetadata { Alias = "Queue Normal", Description = "Collections waiting at normal priority at the end of the period" },
                        ["QueueLengthLow"] = new ColumnMetadata { Alias = "Queue Low", Description = "Collections waiting at low priority at the end of the period - taken when there's nothing at high or normal" },
                        ["Workers"] = new ColumnMetadata { Description = "Workers taking collections off the work queue" },
                        ["BusyWorkers"] = new ColumnMetadata { Alias = "Busy Workers", Description = "Workers processing a collection at the end of the period" },
                        ["MaxBusyWorkers"] = new ColumnMetadata { Alias = "Max Busy Workers", Description = "The most workers processing a collection at once in the period.  Equal to Workers means none were free at some point." },
                        ["QueueItemsStartedPerMin"] = new ColumnMetadata { Alias = "Items Started /min", FormatString = "N1", Description = "Collections taken off the work queue by a worker, per minute of the period" },
                        ["QueueSkippedPct"] = new ColumnMetadata { Alias = "Skipped %", FormatString = "N2", Description = "Share of the period's attempts to queue a collection that were skipped because the previous run of the same one was still queued or running - the schedule is more frequent than the service can keep up with.  Above 0 is worth a look." },
                        ["QueueWaitAvgMs"] = new ColumnMetadata { Alias = "Queue Wait Avg ms", Description = "Average time a collection waited in the queue for a worker" },
                        ["QueueWaitMaxMs"] = new ColumnMetadata { Alias = "Queue Wait Max ms", Description = "The longest any collection waited in the queue for a worker" },
                        ["ProcessStartDate"] = new ColumnMetadata { Alias = "Process Start", Description = "When the service process started" },
                        ["Restarts"] = new ColumnMetadata { Description = "Times the service restarted in the period" },
                        ["Samples"] = new ColumnMetadata { Description = "Samples in the period - one a minute by default (ServiceStatsFrequencySeconds), while LogInternalPerformanceCounters is on" }
                    }
                },
                [2] = new CustomReportResult
                {
                    ResultName = "Work Queue by Priority",
                    // Feeds the chart - the same figures are columns of the first result
                    ShowWithAllResults = false,
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["SnapshotDate"] = new ColumnMetadata { Alias = "Time", Description = "The start of the period" },
                        ["Series"] = new ColumnMetadata { Alias = "Priority", Description = "The work queue priority, with the service where there's more than one" },
                        ["QueueLength"] = new ColumnMetadata { Alias = "Queue Length", Description = "Collections waiting at this priority at the end of the period" }
                    }
                }
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@DBADashAgentID", ParamType = "INT" },
                    // Presence of @FromDate/@ToDate is what shows the global date filter on the report
                    new() { ParamName = "@FromDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@ToDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@DateGroupingMin", ParamType = "INT" }
                }
            }
        };

        /// <summary>A line per service for one measure</summary>
        private static CustomReportChart Line(string title, string column, string yAxisLabel, string yAxisFormat, double? minimumMax = null) => new()
        {
            TableIndex = 1,
            Title = title,
            // The axis is the period asked for, so a service that wasn't running for part of it shows the gap
            BindXAxisToDateRange = true,
            Config = new ChartConfiguration
            {
                ChartType = ChartTypes.Line,
                ChartTitle = title,
                XColumn = "SnapshotDate",
                MetricColumn = column,
                SeriesColumn = "Service",
                YAxisLabel = yAxisLabel,
                YAxisFormat = yAxisFormat,
                YAxisMinimumMax = minimumMax,
                GeometrySize = 0
            }
        };
    }
}
