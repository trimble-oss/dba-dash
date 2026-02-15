using DBADashGUI.CustomReports;
using System.Collections.Generic;

namespace DBADashGUI.Performance
{
    public class ResourceGovernorResourcePoolsReport
    {
        private static List<CellHighlightingRule> WarningIfCreaterThanZeroGreenIfZero => new List<CellHighlightingRule>
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
                ResultName = "Resource Governor Resource Pools",
                Columns = new Dictionary<string, ColumnMetadata>
                {
                    { "ResourcePoolID", new ColumnMetadata { Alias = "ResourcePoolID", Visible = false, Description = "DBADash ID for the resource pool" } },
                    { "InstanceID", new ColumnMetadata { Alias = "Instance ID", Visible = false } },
                    { "pool_id", new ColumnMetadata { Alias = "Pool ID", Description = "The ID of the resource pool" } },
                    { "name", new ColumnMetadata { Alias = "Name", Description = "The name of the resource pool" } },

                    // Period metrics
                    { "period_cpu_usage_ms", new ColumnMetadata { Alias = "Period CPU Usage (ms)", Description = "CPU usage in milliseconds during the period" , FormatString = "N1"} },
                    { "period_cpu_cores", new ColumnMetadata { Alias = "Period CPU Cores", Description = "Average CPU cores used during the period", FormatString = "N1" } },
                    { "period_cpu_percent", new ColumnMetadata { Alias = "Period CPU %", Description = "CPU usage as a percentage during the period", FormatString = "N1" } },
                    { "period_cpu_share_percent", new ColumnMetadata { Alias = "Period CPU Share %", Description = "Percentage share of total CPU usage during the period", FormatString = "N1" } },
                    { "cpu_cap_near_threshold_percent", new ColumnMetadata { Alias = "CPU Near Cap %", Description = "Percentage of time CPU usage was at or above 95% of the cap (throttling indicator)", FormatString = "P1",
                        Highlighting = new CellHighlightingRuleSet("cpu_cap_near_threshold_percent")
                        {
                            Rules = new List<CellHighlightingRule>
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
                                    Value1 = "0.05", // Warning if > 5%
                                    Status = DBADashStatus.DBADashStatusEnum.Warning
                                },
                                new()
                                {
                                    ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan,
                                    Value1 = "0.5", // Critical if > 50%
                                    Status = DBADashStatus.DBADashStatusEnum.Critical
                                }
                            }
                        },} },
                    { "period_memgrant_count_per_min", new ColumnMetadata { Alias = "Period Memory Grants/Min", Description = "Memory grants per minute during the period", FormatString = "N1" } },
                    { "period_memgrant_timeout_count_per_min", new ColumnMetadata { Alias = "Period Memory Grant Timeouts/Min", Description = "Memory grant timeouts per minute during the period", FormatString = "N1",
                        Highlighting = new CellHighlightingRuleSet("period_memgrant_timeout_count_per_min")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "period_out_of_memory_count_per_min", new ColumnMetadata { Alias = "Period Out of Memory/Min", Description = "Out of memory events per minute during the period", FormatString = "N1",
                        Highlighting = new CellHighlightingRuleSet("period_out_of_memory_count_per_min")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "period_read_io_queued_per_min", new ColumnMetadata { Alias = "Period Read I/O Queued/Min", Description = "Read I/Os queued per minute during the period", FormatString = "N1" } },
                    { "period_read_io_issued_per_min", new ColumnMetadata { Alias = "Period Read I/O Issued/Min", Description = "Read I/Os issued per minute during the period", FormatString = "N1" } },
                    { "period_read_io_completed_per_min", new ColumnMetadata { Alias = "Period Read I/O Completed/Min", Description = "Read I/Os completed per minute during the period", FormatString = "N1" } },
                    { "period_read_io_throttled_per_min", new ColumnMetadata { Alias = "Period Read I/O Throttled/Min", Description = "Read I/Os throttled per minute during the period", FormatString = "N1",
                        Highlighting = new CellHighlightingRuleSet("period_read_io_throttled_per_min")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "period_read_mb_per_sec", new ColumnMetadata { Alias = "Period Read MB/Sec", Description = "Read throughput in MB per second during the period", FormatString = "N1" } },
                    { "period_read_io_stall_ms_per_min", new ColumnMetadata { Alias = "Period Read I/O Stall ms/Min", Description = "Read I/O stall time in milliseconds per minute during the period", FormatString = "N1" } },
                    { "period_read_io_stall_queued_ms_per_min", new ColumnMetadata { Alias = "Period Read I/O Stall Queued ms/Min", Description = "Read I/O stall queued time in milliseconds per minute during the period. Delay introduced by I/O Resource Governance", FormatString = "N1",
                        Highlighting = new CellHighlightingRuleSet("period_read_io_stall_queued_ms_per_min")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "period_write_io_queued_per_min", new ColumnMetadata { Alias = "Period Write I/O Queued/Min", Description = "Write I/Os queued per minute during the period", FormatString = "N1" } },
                    { "period_write_io_issued_per_min", new ColumnMetadata { Alias = "Period Write I/O Issued/Min", Description = "Write I/Os issued per minute during the period", FormatString = "N1" } },
                    { "period_write_io_completed_per_min", new ColumnMetadata { Alias = "Period Write I/O Completed/Min", Description = "Write I/Os completed per minute during the period", FormatString = "N1" } },
                    { "period_write_io_throttled_per_min", new ColumnMetadata { Alias = "Period Write I/O Throttled/Min", Description = "Write I/Os throttled per minute during the period", FormatString = "N1",
                        Highlighting = new CellHighlightingRuleSet("period_write_io_throttled_per_min")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "period_write_mb_per_sec", new ColumnMetadata { Alias = "Period Write MB/Sec", Description = "Write throughput in MB per second during the period" , FormatString = "N1"} },
                    { "period_write_io_stall_ms_per_min", new ColumnMetadata { Alias = "Period Write I/O Stall ms/Min", Description = "Write I/O stall time in milliseconds per minute during the period", FormatString = "N1" } },
                    { "period_write_io_stall_queued_ms_per_min", new ColumnMetadata { Alias = "Period Write I/O Stall Queued ms/Min", Description = "Write I/O stall queued time in milliseconds per minute during the period. Delay introduced by I/O Resource Governance", FormatString = "N1",
                        Highlighting = new CellHighlightingRuleSet("period_write_io_stall_queued_ms_per_min")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "period_io_issue_delay_ms_per_min", new ColumnMetadata { Alias = "Period I/O Issue Delay ms/Min", Description = "I/O issue delay in milliseconds per minute during the period. Delay between scheduled and actual I/O issue", FormatString = "N1",
                        Highlighting = new CellHighlightingRuleSet("period_io_issue_delay_ms_per_min")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "period_io_issue_delay_non_throttled_ms_per_min", new ColumnMetadata { Alias = "Period I/O Issue Delay Non-Throttled ms/Min", Description = "Non-throttled I/O issue delay in milliseconds per minute during the period", FormatString = "N1" } },
                    { "period_cpu_delayed_ms_per_min", new ColumnMetadata { Alias = "Period CPU Delayed ms/Min", Description = "CPU delay time in milliseconds per minute during the period", FormatString = "N1" } },
                    { "period_cpu_active_ms_per_min", new ColumnMetadata { Alias = "Period CPU Active ms/Min", Description = "CPU active time in milliseconds per minute during the period", FormatString = "N1" } },
                    { "period_cpu_violation_delay_ms_per_min", new ColumnMetadata { Alias = "Period CPU Violation Delay ms/Min", Description = "CPU violation delay in milliseconds per minute during the period. CPU time delay below minimum guarantee", FormatString = "N1",
                        Highlighting = new CellHighlightingRuleSet("period_cpu_violation_delay_ms_per_min")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "period_cpu_violation_sec_per_min", new ColumnMetadata { Alias = "Period CPU Violation Sec/Min", Description = "CPU violation time in seconds per minute during the period. Time spent in CPU violation state", FormatString = "N1",
                        Highlighting = new CellHighlightingRuleSet("period_cpu_violation_sec_per_min")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "period_cpu_usage_preemptive_ms_per_min", new ColumnMetadata { Alias = "Period CPU Preemptive ms/Min", Description = "Preemptive CPU usage in milliseconds per minute during the period", FormatString = "N1" } },

                    // Current state and cumulative statistics
                    { "statistics_start_time", new ColumnMetadata { Alias = "Statistics Start Time", Description = "The time when statistics was reset for this pool", FormatString= "G"   } },
                    { "SnapshotDate", new ColumnMetadata { Alias = "Snapshot Date", Description = "The last time the ResourceGovernorResourcePools collection ran (sys.dm_resource_governor_resource_pools)", FormatString= "G"   } },
                    { "total_cpu_usage_ms", new ColumnMetadata { Alias = "Total CPU Usage (ms)", Description = "The cumulative CPU usage in milliseconds since the resource governor statistics were reset" } },
                    { "cache_memory_kb", new ColumnMetadata { Alias = "Cache Memory (KB)", Description = "The current total cache memory usage in kilobytes" } },
                    { "compile_memory_kb", new ColumnMetadata { Alias = "Compile Memory (KB)", Description = "The current total stolen memory usage in kilobytes. Most this usage would be for compile and optimization" } },
                    { "used_memgrant_kb", new ColumnMetadata { Alias = "Used Memory Grant (KB)", Description = "The current total used (stolen) memory for memory grants" } },
                    { "total_memgrant_count", new ColumnMetadata { Alias = "Total Memory Grant Count", Description = "The cumulative count of memory grants in this resource pool" } },
                    { "total_memgrant_timeout_count", new ColumnMetadata { Alias = "Total Memory Grant Timeout Count", Description = "The cumulative count of memory grant time-outs in this resource pool",
                        Highlighting = new CellHighlightingRuleSet("total_memgrant_timeout_count")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "active_memgrant_count", new ColumnMetadata { Alias = "Active Memory Grant Count", Description = "The current count of memory grants" } },
                    { "active_memgrant_kb", new ColumnMetadata { Alias = "Active Memory Grant (KB)", Description = "The sum, in kilobytes, of current memory grants" } },
                    { "memgrant_waiter_count", new ColumnMetadata { Alias = "Memory Grant Waiter Count", Description = "The count of queries currently pending on memory grants. Indicates memory pressure",
                        Highlighting = new CellHighlightingRuleSet("memgrant_waiter_count")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "max_memory_kb", new ColumnMetadata { Alias = "Max Memory (KB)", Description = "The maximum amount of memory, in kilobytes, that the resource pool can use as query workspace memory" } },
                    { "used_memory_kb", new ColumnMetadata { Alias = "Used Memory (KB)", Description = "The amount of query workspace memory used, in kilobytes, for the resource pool" } },
                    { "target_memory_kb", new ColumnMetadata { Alias = "Target Memory (KB)", Description = "The target amount of query workspace memory, in kilobytes, the resource pool is trying to attain" } },
                    { "out_of_memory_count", new ColumnMetadata { Alias = "Out of Memory Count", Description = "The number of failed memory allocations in the pool since the resource governor statistics were reset",
                        Highlighting = new CellHighlightingRuleSet("out_of_memory_count")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },

                    // Configuration settings
                    { "min_cpu_percent", new ColumnMetadata { Alias = "Min CPU %", Description = "The current configuration for the guaranteed average CPU bandwidth for all requests in the resource pool when there's CPU contention" } },
                    { "max_cpu_percent", new ColumnMetadata { Alias = "Max CPU %", Description = "The current configuration for the maximum average CPU bandwidth allowed for all requests in the resource pool when there's CPU contention" } },
                    { "min_memory_percent", new ColumnMetadata { Alias = "Min Memory %", Description = "The current configuration for the guaranteed amount of memory for all requests in the resource pool when there's memory contention" } },
                    { "max_memory_percent", new ColumnMetadata { Alias = "Max Memory %", Description = "The current configuration for the percentage of total server memory that can be used by requests in this resource pool" } },
                    { "cap_cpu_percent", new ColumnMetadata { Alias = "Cap CPU %", Description = "Hard cap on the CPU bandwidth that all requests in the resource pool receive" } },
                    { "min_iops_per_volume", new ColumnMetadata { Alias = "Min IOPS Per Volume", Description = "The minimum I/O per second (IOPS) per disk volume setting for this pool" } },
                    { "max_iops_per_volume", new ColumnMetadata { Alias = "Max IOPS Per Volume", Description = "The maximum I/O per second (IOPS) per disk volume setting for this pool" } },

                    // I/O statistics
                    { "read_io_queued_total", new ColumnMetadata { Alias = "Read I/O Queued Total", Description = "The total read I/Os enqueued since the resource governor statistics were reset" } },
                    { "read_io_issued_total", new ColumnMetadata { Alias = "Read I/O Issued Total", Description = "The total read I/Os issued since the resource governor statistics were reset" } },
                    { "read_io_completed_total", new ColumnMetadata { Alias = "Read I/O Completed Total", Description = "The total read I/Os completed since the resource governor statistics were reset" } },
                    { "read_io_throttled_total", new ColumnMetadata { Alias = "Read I/O Throttled Total", Description = "The total read I/Os throttled since the resource governor statistics were reset",
                        Highlighting = new CellHighlightingRuleSet("read_io_throttled_total")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "read_bytes_total", new ColumnMetadata { Alias = "Read Bytes Total", Description = "The total number of bytes read since the resource governor statistics were reset" } },
                    { "read_io_stall_total_ms", new ColumnMetadata { Alias = "Read I/O Stall Total (ms)", Description = "Total time (in milliseconds) between read I/O arrival and completion" } },
                    { "read_io_stall_queued_ms", new ColumnMetadata { Alias = "Read I/O Stall Queued (ms)", Description = "Total time (in milliseconds) between read I/O arrival and issue. Delay introduced by I/O Resource Governance",
                        Highlighting = new CellHighlightingRuleSet("read_io_stall_queued_ms")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "write_io_queued_total", new ColumnMetadata { Alias = "Write I/O Queued Total", Description = "The total write I/Os enqueued since the resource governor statistics were reset" } },
                    { "write_io_issued_total", new ColumnMetadata { Alias = "Write I/O Issued Total", Description = "The total write I/Os issued since the resource governor statistics were reset" } },
                    { "write_io_completed_total", new ColumnMetadata { Alias = "Write I/O Completed Total", Description = "The total write I/Os completed since the resource governor statistics were reset" } },
                    { "write_io_throttled_total", new ColumnMetadata { Alias = "Write I/O Throttled Total", Description = "The total write I/Os throttled since the resource governor statistics were reset",
                        Highlighting = new CellHighlightingRuleSet("write_io_throttled_total")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "write_bytes_total", new ColumnMetadata { Alias = "Write Bytes Total", Description = "The total number of bytes written since the resource governor statistics were reset" } },
                    { "write_io_stall_total_ms", new ColumnMetadata { Alias = "Write I/O Stall Total (ms)", Description = "Total time (in milliseconds) between write I/O arrival and completion" } },
                    { "write_io_stall_queued_ms", new ColumnMetadata { Alias = "Write I/O Stall Queued (ms)", Description = "Total time (in milliseconds) between write I/O arrival and issue. This is the delay introduced by I/O Resource Governance" } },
                    { "io_issue_violations_total", new ColumnMetadata { Alias = "I/O Issue Violations Total", Description = "Total I/O issue violations. That is, the number of times when the rate of I/O issue was lower than the reserved rate",
                        Highlighting = new CellHighlightingRuleSet("io_issue_violations_total")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "io_issue_delay_total_ms", new ColumnMetadata { Alias = "I/O Issue Delay Total (ms)", Description = "Total time (in milliseconds) between the scheduled issue and actual issue of I/O",
                        Highlighting = new CellHighlightingRuleSet("io_issue_delay_total_ms")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "io_issue_delay_non_throttled_total_ms", new ColumnMetadata { Alias = "I/O Issue Delay Non-Throttled Total (ms)", Description = "Total time (in milliseconds) between the scheduled issue and actual issue of a non-throttled I/O" } },

                    // CPU statistics
                    { "total_cpu_delayed_ms", new ColumnMetadata { Alias = "Total CPU Delayed (ms)", Description = "Total time (in milliseconds) between when a runnable worker yields, and when the operating system gives back control to another runnable worker" } },
                    { "total_cpu_active_ms", new ColumnMetadata { Alias = "Total CPU Active (ms)", Description = "Total active CPU time (in milliseconds)" } },
                    { "total_cpu_violation_delay_ms", new ColumnMetadata { Alias = "Total CPU Violation Delay (ms)", Description = "Total CPU violation delays (in milliseconds). That is, total CPU time delay that was lower than the minimum guaranteed delay",
                        Highlighting = new CellHighlightingRuleSet("total_cpu_violation_delay_ms")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "total_cpu_violation_sec", new ColumnMetadata { Alias = "Total CPU Violation (sec)", Description = "Total CPU violations (in seconds). That is, total time accrued when a CPU time violation was in-flight",
                        Highlighting = new CellHighlightingRuleSet("total_cpu_violation_sec")
                        {
                            Rules = WarningIfCreaterThanZeroGreenIfZero
                        },} },
                    { "total_cpu_usage_preemptive_ms", new ColumnMetadata { Alias = "Total CPU Preemptive (ms)", Description = "Total CPU time used while in preemptive mode scheduling for the workload group (in milliseconds)" } },
                }
            };
            return result.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing().SetDisplayIndexBasedOnColumnOrder();
        }
    }
}