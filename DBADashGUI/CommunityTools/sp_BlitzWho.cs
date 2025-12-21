using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_BlitzWho
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzWho.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzWho.ToString(),
            URL = CommunityTools.FirstResponderKitUrl,
            Description = "Shows what queries are currently running",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@ShowSleepingSPIDs", ParamType = "TINYINT" },
                    new() { ParamName = "@ExpertMode", ParamType = "BIT" },
                    new() { ParamName = "@MinElapsedSeconds", ParamType = "INT" },
                    new() { ParamName = "@MinCPUTime", ParamType = "INT" },
                    new() { ParamName = "@MinLogicalReads", ParamType = "INT" },
                    new() { ParamName = "@MinPhysicalReads", ParamType = "INT" },
                    new() { ParamName = "@MinWrites", ParamType = "INT" },
                    new() { ParamName = "@MinTempdbMB", ParamType = "INT" },
                    new() { ParamName = "@MinRequestedMemoryKB", ParamType = "INT" },
                    new() { ParamName = "@MinBlockingSeconds", ParamType = "INT" },
                    new() { ParamName = "@ShowActualParameters", ParamType = "BIT" },
                    new() { ParamName = "@GetOuterCommand", ParamType = "BIT" },
                    new() { ParamName = "@GetLiveQueryPlan", ParamType = "BIT" },
                    new() { ParamName = "@SortOrder", ParamType = "NVARCHAR" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@ShowSleepingSPIDs",
                    Name = "Show Sleeping SPIDs",
                    DefaultValue = "0",
                    PickerItems = new()
                    {
                        { 0, "No" },
                        { 1, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@ExpertMode",
                    Name = "Expert Mode",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@ShowActualParameters",
                    Name = "Show Actual Parameters",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@GetOuterCommand",
                    Name = "Get Outer Command",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@GetLiveQueryPlan",
                    Name = "Get Live Query Plan",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@SortOrder",
                    Name = "Sort Order",
                    DefaultValue = "elapsed time",
                    PickerItems = new()
                    {
                        { "client_interface_name", "client_interface_name" },
                        { "database_name", "database_name" },
                        { "deadlock_priority", "deadlock_priority" },
                        { "elapsed time", "elapsed time" },
                        { "grant", "grant" },
                        { "grant_memory_kb", "grant_memory_kb" },
                        { "ideal_memory_kb", "ideal_memory_kb" },
                        { "is_implicit_transaction", "is_implicit_transaction" },
                        { "login_name", "login_name" },
                        { "memory_usage", "memory_usage" },
                        { "open_transaction_count", "open_transaction_count" },
                        { "program_name", "program_name" },
                        { "query_cost", "query_cost" },
                        { "query_memory_grant_used_memory_kb", "query_memory_grant_used_memory_kb" },
                        { "request_cpu_time", "request_cpu_time" },
                        { "request_logical_reads", "request_logical_reads" },
                        { "request_physical_reads", "request_physical_reads" },
                        { "request_writes", "request_writes" },
                        { "requested_memory_kb", "requested_memory_kb" },
                        { "resource_pool_name", "resource_pool_name" },
                        { "session_cpu", "session_cpu" },
                        { "session_id", "session_id" },
                        { "session_logical_reads", "session_logical_reads" },
                        { "session_physical_reads", "session_physical_reads" },
                        { "session_writes", "session_writes" },
                        { "tempdb_allocations_mb", "tempdb_allocations_mb" },
                        { "transaction_isolation_level", "transaction_isolation_level" },
                        { "workload_group_name", "workload_group_name" },
                    }
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            {
                                "query_plan",
                                new ColumnMetadata
                                {
                                    Link = new QueryPlanLinkColumnInfo
                                    {
                                        TargetColumn = "query_plan"
                                    }
                                }
                            },
                            {
                                "live_query_plan",
                                new ColumnMetadata
                                {
                                    Link = new QueryPlanLinkColumnInfo
                                    {
                                        TargetColumn = "live_query_plan"
                                    }
                                }
                            },
                            {
                                "query_text",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "query_text",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "outer_command",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "outer_command",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "fix_parameter_sniffing",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "fix_parameter_sniffing",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            }
                        }
                    }
                }
            },
        };
    }
}