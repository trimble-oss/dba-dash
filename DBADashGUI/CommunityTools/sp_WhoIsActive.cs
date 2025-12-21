using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_WhoIsActive
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_WhoIsActive.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_WhoIsActive.ToString(),
            URL = "https://whoisactive.com/",
            Description = "Shows what queries are currently running",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@filter", ParamType = "NVARCHAR" },
                    new() { ParamName = "@filter_type", ParamType = "VARCHAR" },
                    new() { ParamName = "@not_filter", ParamType = "NVARCHAR" },
                    new() { ParamName = "@not_filter_type", ParamType = "VARCHAR" },
                    new() { ParamName = "@show_own_spid", ParamType = "BIT" },
                    new() { ParamName = "@show_system_spids", ParamType = "BIT" },
                    new() { ParamName = "@show_sleeping_spids", ParamType = "TINYINT" },
                    new() { ParamName = "@get_full_inner_text", ParamType = "BIT" },
                    new() { ParamName = "@get_plans", ParamType = "TINYINT" },
                    new() { ParamName = "@get_outer_command", ParamType = "BIT" },
                    new() { ParamName = "@get_transaction_info", ParamType = "BIT" },
                    new() { ParamName = "@get_task_info", ParamType = "TINYINT" },
                    new() { ParamName = "@get_locks", ParamType = "BIT" },
                    new() { ParamName = "@get_avg_time", ParamType = "BIT" },
                    new() { ParamName = "@get_additional_info", ParamType = "BIT" },
                    new() { ParamName = "@get_memory_info", ParamType = "BIT" },
                    new() { ParamName = "@find_block_leaders", ParamType = "BIT" },
                    new() { ParamName = "@delta_interval", ParamType = "INT" },
                    new() { ParamName = "@output_column_list", ParamType = "NVARCHAR" },
                    new() { ParamName = "@sort_order", ParamType = "NVARCHAR" },
                    new() { ParamName = "@format_output", ParamType = "TINYINT" },
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "query_plan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "query_plan"
                                }
                            },
                            {
                                "sql_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "sql_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "sql_command",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "sql_command",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "additional_info",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "additional_info",
                                    TextHandling = CodeEditor.CodeEditorModes.XML
                                }
                            },
                            {
                                "locks",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "locks",
                                    TextHandling = CodeEditor.CodeEditorModes.XML
                                }
                            },
                            {
                                "memory_info",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "memory_info",
                                    TextHandling = CodeEditor.CodeEditorModes.XML
                                }
                            },
                        }
                    }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@show_own_spid",
                    Name = "Show Own SPID",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@show_system_spids",
                    Name = "Show System SPIDs",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@show_sleeping_spids ",
                    Name = "Show Sleeping SPIDs",
                    DefaultValue = 1,
                    PickerItems = new()
                    {
                        { 0, "No" },
                        { 1, "Sleeping SPIDs with open transaction (Default)" },
                        { 2, "All Sleeping SPIDs" }
                    }
                },
                new()
                {
                    ParameterName = "@get_full_inner_text",
                    Name = "Get Full Inner Text",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@get_plans",
                    Name = "Get Plans",
                    DefaultValue = 0,
                    PickerItems = new()
                    {
                        { 0, "No" },
                        { 1, "Statement Plan" },
                        { 2, "Full Plan" }
                    }
                },
                new()
                {
                    ParameterName = "@get_outer_command ",
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
                    ParameterName = "@get_transaction_info",
                    Name = "Get Transaction Info",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@get_task_info",
                    Name = "Get Task Info",
                    DefaultValue = 1,
                    PickerItems = new()
                    {
                        { 0, "No task related info" },
                        { 1, "Lightweight mode (default)" },
                        { 2, "Full" }
                    }
                },
                new()
                {
                    ParameterName = "@get_locks",
                    Name = "Get Locks",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@get_avg_time",
                    Name = "Get Avg Time",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@get_additional_info",
                    Name = "Get Additional Info",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@get_memory_info",
                    Name = "Get Memory Info",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@find_block_leaders",
                    Name = "Find Block Leaders",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@format_output",
                    Name = "Format Output",
                    DefaultValue = 0,
                    PickerItems = new()
                    {
                        { 0, "No" },
                        { 1, "Yes" }
                    }
                },
                new()
                {
                    ParameterName = "@delta_interval",
                    Name = "Delta Interval",
                    DefaultValue = 0,
                    PickerItems = new()
                    {
                        { 0, "0" },
                        { 5, "5" },
                        { 10, "10" },
                        { 15, "15" },
                        { 20, "20" },
                        { 30, "30" },
                        { 40, "40" },
                        { 60, "60" }
                    }
                },
                new()
                {
                    ParameterName = "@sort_order",
                    Name = "Sort Order",
                    DefaultValue = "[start_time] ASC",
                    PickerItems = new()
                    {
                        { "[session_id] ASC", "session_id ASC" },
                        { "[session_id] DESC", "session_id DESC" },
                        { "[physical_io] ASC", "physical_io ASC" },
                        { "[physical_io] DESC", "physical_io DESC" },
                        { "[reads] ASC", "reads ASC" },
                        { "[reads] DESC", "reads DESC" },
                        { "[physical_reads] ASC", "physical_reads ASC" },
                        { "[physical_reads] DESC", "physical_reads DESC" },
                        { "[writes] ASC", "writes ASC" },
                        { "[writes] DESC", "writes DESC" },
                        { "[tempdb_allocations] ASC", "tempdb_allocations ASC" },
                        { "[tempdb_allocations] DESC", "tempdb_allocations DESC" },
                        { "[tempdb_current] ASC", "tempdb_current ASC" },
                        { "[tempdb_current] DESC", "tempdb_current DESC" },
                        { "[CPU] ASC", "CPU ASC" },
                        { "[CPU] DESC", "CPU DESC" },
                        { "[context_switches] ASC", "context_switches ASC" },
                        { "[context_switches] DESC", "context_switches DESC" },
                        { "[used_memory] ASC", "used_memory ASC" },
                        { "[used_memory] DESC", "used_memory DESC" },
                        { "[physical_io_delta] ASC", "physical_io_delta ASC" },
                        { "[physical_io_delta] DESC", "physical_io_delta DESC" },
                        { "[reads_delta] ASC", "reads_delta ASC" },
                        { "[reads_delta] DESC", "reads_delta DESC" },
                        { "[physical_reads_delta] ASC", "physical_reads_delta ASC" },
                        { "[physical_reads_delta] DESC", "physical_reads_delta DESC" },
                        { "[writes_delta] ASC", "writes_delta ASC" },
                        { "[writes_delta] DESC", "writes_delta DESC" },
                        { "[tempdb_allocations_delta] ASC", "tempdb_allocations_delta ASC" },
                        { "[tempdb_allocations_delta] DESC", "tempdb_allocations_delta DESC" },
                        { "[tempdb_current_delta] ASC", "tempdb_current_delta ASC" },
                        { "[tempdb_current_delta] DESC", "tempdb_current_delta DESC" },
                        { "[CPU_delta] ASC", "CPU_delta ASC" },
                        { "[CPU_delta] DESC", "CPU_delta DESC" },
                        { "[context_switches_delta] ASC", "context_switches_delta ASC" },
                        { "[context_switches_delta] DESC", "context_switches_delta DESC" },
                        { "[used_memory_delta] ASC", "used_memory_delta ASC" },
                        { "[used_memory_delta] DESC", "used_memory_delta DESC" },
                        { "[tasks] ASC", "tasks ASC" },
                        { "[tasks] DESC", "tasks DESC" },
                        { "[tran_start_time] ASC", "tran_start_time ASC" },
                        { "[tran_start_time] DESC", "tran_start_time DESC" },
                        { "[open_tran_count] ASC", "open_tran_count ASC" },
                        { "[open_tran_count] DESC", "open_tran_count DESC" },
                        { "[blocking_session_id] ASC", "blocking_session_id ASC" },
                        { "[blocking_session_id] DESC", "blocking_session_id DESC" },
                        { "[blocked_session_count] ASC", "blocked_session_count ASC" },
                        { "[blocked_session_count] DESC", "blocked_session_count DESC" },
                        { "[percent_complete] ASC", "percent_complete ASC" },
                        { "[percent_complete] DESC", "percent_complete DESC" },
                        { "[host_name] ASC", "host_name ASC" },
                        { "[host_name] DESC", "host_name DESC" },
                        { "[login_name] ASC", "login_name ASC" },
                        { "[login_name] DESC", "login_name DESC" },
                        { "[database_name] ASC", "database_name ASC" },
                        { "[database_name] DESC", "database_name DESC" },
                        { "[start_time] ASC", "start_time ASC" },
                        { "[start_time] DESC", "start_time DESC" },
                        { "[login_time] ASC", "login_time ASC" },
                        { "[login_time] DESC", "login_time DESC" },
                        { "[program_name] ASC", "program_name ASC" },
                        { "[program_name] DESC", "program_name DESC" },
                    }
                },
            }
        };
    }
}