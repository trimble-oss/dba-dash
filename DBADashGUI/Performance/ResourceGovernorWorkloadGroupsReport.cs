using DBADashGUI.CustomReports;
using System.Collections.Generic;

namespace DBADashGUI.Performance
{
    public class ResourceGovernorWorkloadGroupsReport
    {
        private static List<CellHighlightingRule> WarningIfGreaterThanZeroGreenIfZero => new List<CellHighlightingRule>
                            {
                                new()
                                {
                                    ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                    Value1 = "0",
                                    Status = DBADashStatus.DBADashStatusEnum.OK
                                },
                                new()
                                {
                                    ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan,
                                    Value1 = "0",
                                    Status = DBADashStatus.DBADashStatusEnum.Warning
                                }
                            };

        public static CustomReportResult GetReportResult()
        {
            var result = new CustomReportResult
            {
                ResultName = "Resource Governor Workload Groups",
                Columns = new Dictionary<string, ColumnMetadata>
                {
                    { "InstanceID", new ColumnMetadata { Alias = "Instance ID", Visible = false } },
                    { "name", new ColumnMetadata { Alias = "Name", Description = "Resource governor workload group name" } },
                    { "pool_name", new ColumnMetadata { Alias = "Pool", Description = "Associated resource governor resource pool" } },
                    { "group_id", new ColumnMetadata { Alias = "Group ID", Visible = false, Description = "ID of the workload group" } },
                    { "pool_id", new ColumnMetadata { Alias = "Pool ID", Visible = false, Description = "ID of the resource pool" } },
                    { "external_pool_id", new ColumnMetadata { Alias = "External Pool ID", Visible = false, Description = "ID of the external resource pool" } },
                    { "period_cpu_usage_ms", new ColumnMetadata {
                        Alias = "Period CPU Usage (ms)",
                        FormatString = "N0",
                        Description = "Total CPU consumption in ms within the selected date range"
                    } },
                    { "period_cpu_cores", new ColumnMetadata {
                        Alias = "Period CPU Cores",
                        FormatString = "N2",
                        Description = "Average CPU cores used within the selected date range"
                    } },
                    { "period_cpu_percent", new ColumnMetadata {
                        Alias = "Period CPU Percent",
                        FormatString = "P2",
                        Description = "Percent of total CPU capacity used within the selected date range"
                    } },
                    { "period_cpu_share_percent", new ColumnMetadata {
                        Alias = "Period CPU Share Percent",
                        FormatString = "P2",
                        Description = "Percent of CPU used in relation to other workload groups within the selected date range"
                    } },
                    { "period_requests_per_min", new ColumnMetadata {
                        Alias = "Period Requests Per Min",
                        FormatString = "N2",
                        Description = "Count of requests completed per minute within the selected date range"
                    } },
                    { "period_queued_request_count_per_min", new ColumnMetadata {
                        Alias = "Period Queued Request Count Per Min",
                        FormatString = "N2",
                        Highlighting = new CellHighlightingRuleSet("period_queued_request_count_per_min")
                        {
                            Rules = WarningIfGreaterThanZeroGreenIfZero
                        },
                        Description = "Count of queued requests per minute within the selected date range. A non-zero value means the GROUP_MAX_REQUESTS limit was reached."
                    } },
                    { "period_cpu_limit_violations_per_min", new ColumnMetadata {
                        Alias = "Period CPU Limit Violations Per Min",
                        FormatString = "N2",
                        Highlighting = new CellHighlightingRuleSet("period_cpu_limit_violations_per_min")
                        {
                            Rules = WarningIfGreaterThanZeroGreenIfZero
                        },
                        Description = "Count of requests exceeding the CPU limit per minute within the selected date range."
                    } },
                    { "period_lock_waits_per_min", new ColumnMetadata {
                        Alias = "Period Lock Waits Per Min",
                        FormatString = "N2",
                        Description = "Count of lock waits that occurred per minute within the selected date range"
                    } },
                    { "period_lock_wait_time_ms_per_sec", new ColumnMetadata {
                        Alias = "Period Lock Wait Time MS Per Sec",
                        FormatString = "N2",
                        Description = "Lock wait time in milliseconds per second within the selected date range"
                    } },
                    { "period_query_optimizations_per_min", new ColumnMetadata {
                        Alias = "Period Query Optimizations Per Min",
                        FormatString = "N2",
                        Description = "Count of query optimizations within the selected date range"
                    } },
                    { "period_suboptimal_plan_generation_count_per_min", new ColumnMetadata {
                        Alias = "Period Suboptimal Plan Generation Count Per Min",
                        FormatString = "N2",
                        Description = "Count of suboptimal plan generations that occurred due to memory pressure within the selected date range."
                    } },
                    { "period_reduced_memgrant_count_per_min", new ColumnMetadata {
                        Alias = "Period Reduced Memgrant Count Per Min",
                        FormatString = "N2",
                        Description = "Count of memory grants that reached the per-request memory grant size within the selected date range.",
                        Highlighting = new CellHighlightingRuleSet("period_reduced_memgrant_count_per_min")
                        {
                            Rules = WarningIfGreaterThanZeroGreenIfZero
                        },
                    } },
                    { "period_cpu_usage_preemptive_ms_per_min", new ColumnMetadata {
                        Alias = "Period CPU Usage Preemptive MS Per Min",
                        FormatString = "N2",
                        Description = "Period CPU time used while in preemptive mode scheduling in milliseconds per minute (e.g. extended stored procedures, distributed queries)"
                    } },
                    { "period_tempdb_data_limit_violations_per_min", new ColumnMetadata {
                        Alias = "Period Tempdb Data Limit Violations Per Min",
                        FormatString = "N2",
                        Highlighting = new CellHighlightingRuleSet("period_tempdb_data_limit_violations_per_min")
                        {
                            Rules = WarningIfGreaterThanZeroGreenIfZero
                        },
                        Description = "The number of queries aborted per minute because they exceeded the limit on tempdb space for the workload group"
                    } },
                    { "period_avg_active_request_count", new ColumnMetadata {
                        Alias = "Period Avg Active Request Count",
                        FormatString = "N2",
                        Description = "Average 'active_request_count' for the selected date range."
                    } },
                    { "period_avg_queued_request_count", new ColumnMetadata {
                        Alias = "Period Avg Queued Request Count",
                        FormatString = "N2",
                        Description = "Average 'queued_request_count' for the selected date range."
                    } },
                    { "period_avg_blocked_task_count", new ColumnMetadata {
                        Alias = "Period Avg Blocked Task Count",
                        FormatString = "N2",
                        Description = "Average 'blocked_task_count' for the selected date range."
                    } },
                    { "period_avg_active_parallel_thread_count", new ColumnMetadata {
                        Alias = "Period Avg Active Parallel Thread Count",
                        FormatString = "N2",
                        Description = "Average 'avg_active_parallel_thread_count' for the selected date range."
                    } },
                    { "period_avg_tempdb_data_space_kb", new ColumnMetadata {
                        Alias = "Period Avg Tempdb Data Space KB",
                        FormatString = "N0",
                        Description = "Average 'avg_tempdb_data_space_kb' for the selected date range."
                    } },
                    { "statistics_start_time", new ColumnMetadata {
                        Alias = "Statistics Start Time",
                        Description = "The time when statistics collection for the workload group started",
                        FormatString= "G"
                    } },
                    { "SnapshotDate", new ColumnMetadata {
                        Alias = "Snapshot Date",
                        Description = "Date/Time of last collection from sys.dm_resource_governor_workload_groups",
                        FormatString= "G"
                    } },
                    { "total_request_count", new ColumnMetadata {
                        Alias = "Total Request Count",
                        FormatString = "N0",
                        Description = "Cumulative count of completed requests in the workload group"
                    } },
                    { "total_queued_request_count", new ColumnMetadata {
                        Alias = "Total Queued Request Count",
                        FormatString = "N0",
                        Description = "Cumulative count of requests queued after the GROUP_MAX_REQUESTS limit was reached",
                        Highlighting = new CellHighlightingRuleSet("total_queued_request_count")
                        {
                            Rules = WarningIfGreaterThanZeroGreenIfZero
                        },
                    } },
                    { "active_request_count", new ColumnMetadata {
                        Alias = "Active Request Count",
                        FormatString = "N0",
                        Description = "Current request count"
                    } },
                    { "queued_request_count", new ColumnMetadata {
                        Alias = "Queued Request Count",
                        FormatString = "N0",
                        Description = "Current queued request count",
                        Highlighting = new CellHighlightingRuleSet("queued_request_count")
                        {
                            Rules = WarningIfGreaterThanZeroGreenIfZero
                        },
                    } },
                    { "total_cpu_limit_violation_count", new ColumnMetadata {
                        Alias = "Total CPU Limit Violation Count",
                        FormatString = "N0",
                        Description = "Cumulative count of requests exceeding the CPU limit",
                        Highlighting = new CellHighlightingRuleSet("total_cpu_limit_violation_count")
                        {
                            Rules = WarningIfGreaterThanZeroGreenIfZero
                        },
                    } },
                    { "total_cpu_usage_ms", new ColumnMetadata {
                        Alias = "Total CPU Usage (ms)",
                        FormatString = "N0",
                        Description = "Cumulative CPU usage in milliseconds by this workload group"
                    } },
                    { "max_request_cpu_time_ms", new ColumnMetadata {
                        Alias = "Max Request CPU Time MS",
                        FormatString = "N0",
                        Description = "Maximum CPU usage in milliseconds for a single request. This is a measured value, unlike request_max_cpu_time_sec which is a configurable setting."
                    } },
                    { "blocked_task_count", new ColumnMetadata {
                        Alias = "Blocked Task Count",
                        FormatString = "N0",
                        Description = "Current count of blocked tasks"
                    } },
                    { "total_lock_wait_count", new ColumnMetadata {
                        Alias = "Total Lock Wait Count",
                        FormatString = "N0",
                        Description = "Cumulative count of lock waits that occurred"
                    } },
                    { "total_lock_wait_time_ms", new ColumnMetadata {
                        Alias = "Total Lock Wait Time MS",
                        FormatString = "N0",
                        Description = "Cumulative sum of elapsed time in milliseconds that a lock is held"
                    } },
                    { "total_query_optimization_count", new ColumnMetadata {
                        Alias = "Total Query Optimization Count",
                        FormatString = "N0",
                        Description = "Cumulative count of query optimizations in this workload group"
                    } },
                    { "total_suboptimal_plan_generation_count", new ColumnMetadata {
                        Alias = "Total Suboptimal Plan Generation Count",
                        FormatString = "N0",
                        Description = "Cumulative count of suboptimal plan generations that occurred in this workload group due to memory pressure"
                    } },
                    { "total_reduced_memgrant_count", new ColumnMetadata {
                        Alias = "Total Reduced Memgrant Count",
                        FormatString = "N0",
                        Description = "Cumulative count of memory grants that reached the maximum limit on the per-request memory grant size",
                        Highlighting = new CellHighlightingRuleSet("total_reduced_memgrant_count")
                        {
                            Rules = WarningIfGreaterThanZeroGreenIfZero
                        },
                    } },
                    { "max_request_grant_memory_kb", new ColumnMetadata {
                        Alias = "Max Request Grant Memory KB",
                        FormatString = "N0",
                        Description = "Maximum memory grant size in kilobytes of a single request since the statistics were reset"
                    } },
                    { "active_parallel_thread_count", new ColumnMetadata {
                        Alias = "Active Parallel Thread Count",
                        FormatString = "N0",
                        Description = "Current count of parallel thread usage"
                    } },
                    { "importance", new ColumnMetadata {
                        Alias = "Importance",
                        Description = "Current configuration value for the relative importance of a request in this workload group. Importance is one of the following: Low, Medium, or High (default is Medium)"
                    } },
                    { "request_max_memory_grant_percent", new ColumnMetadata {
                        Alias = "Request Max Memory Grant Percent",
                        FormatString = "#,##0'%'",
                        Description = "Current setting for the maximum memory grant as a percentage for a single request"
                    } },
                    { "request_max_cpu_time_sec", new ColumnMetadata {
                        Alias = "Request Max CPU Time Sec",
                        FormatString = "N0",
                        Description = "Current setting for maximum CPU use limit in seconds for a single request"
                    } },
                    { "request_memory_grant_timeout_sec", new ColumnMetadata {
                        Alias = "Request Memory Grant Timeout Sec",
                        FormatString = "N0",
                        Description = "Current setting for memory grant timeout in seconds for a single request"
                    } },
                    { "group_max_requests", new ColumnMetadata {
                        Alias = "Group Max Requests",
                        FormatString = "N0",
                        Description = "Current setting for the maximum number of concurrent requests in the workload group"
                    } },
                    { "max_dop", new ColumnMetadata {
                        Alias = "Max DOP",
                        FormatString = "N0",
                        Description = "Configured maximum degree of parallelism for the workload group. The default value 0 uses global settings."
                    } },
                    { "effective_max_dop", new ColumnMetadata {
                        Alias = "Effective Max DOP",
                        FormatString = "N0",
                        Description = "Effective maximum degree of parallelism for the workload group"
                    } },
                    { "total_cpu_usage_preemptive_ms", new ColumnMetadata {
                        Alias = "Total CPU Usage Preemptive MS",
                        FormatString = "N0",
                        Description = "Total CPU time used while in preemptive mode scheduling in milliseconds for the workload group (e.g. extended stored procedures and distributed queries)"
                    } },
                    { "request_max_memory_grant_percent_numeric", new ColumnMetadata {
                        Alias = "Request Max Memory Grant Percent Numeric",
                        FormatString = "N0",
                        Visible = false,
                        Description = "Current setting for the maximum memory grant as a percentage for a single request (float value). Supports decimal values from 0-100."
                    } },
                    { "tempdb_data_space_kb", new ColumnMetadata {
                        Alias = "Tempdb Data Space KB",
                        FormatString = "N0",
                        Description = "Current data space consumed in the tempdb data files by all sessions in the workload group in kilobytes"
                    } },
                    { "peak_tempdb_data_space_kb", new ColumnMetadata {
                        Alias = "Peak Tempdb Data Space KB",
                        FormatString = "N0",
                        Description = "Peak data space consumed in the tempdb data files by all sessions in the workload group since server startup or since resource governor statistics were reset, in kilobytes"
                    } },
                    { "total_tempdb_data_limit_violation_count", new ColumnMetadata {
                        Alias = "Total Tempdb Data Limit Violation Count",
                        FormatString = "N0",
                        Description = "Number of times a request was aborted because it would exceed the limit on tempdb data space consumption for the workload group"
                    } }
                }
            };
            return result.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing().SetDisplayIndexBasedOnColumnOrder();
        }
    }
}