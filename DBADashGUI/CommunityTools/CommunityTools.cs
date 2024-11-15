using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;
using System.Linq;

namespace DBADashGUI.CommunityTools
{
    internal static class CommunityTools
    {
        private const string FirstResponderKitUrl = "https://github.com/BrentOzarULTD/SQL-Server-First-Responder-Kit";
        private const string ErikDarlingUrl = "https://github.com/erikdarlingdata/DarlingData";

        private static readonly Dictionary<object, string> BooleanPickerItems = new()
        {
            { true, "Yes" },
            { false, "No" },
        };

        public static DirectExecutionReport sp_WhoIsActive = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_WhoIsActive.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_WhoIsActive.ToString(),
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

        public static DirectExecutionReport sp_Blitz = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_Blitz.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_Blitz.ToString(),
            URL = FirstResponderKitUrl,
            Description = "SQL Server Health Check Stored Procedure",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@CheckUserDatabaseObjects", ParamType = "TINYINT" },
                    new() { ParamName = "@CheckProcedureCache", ParamType = "TINYINT" },
                    new() { ParamName = "@OutputType", ParamType = "VARCHAR" },
                    new() { ParamName = "@OutputProcedureCache", ParamType = "TINYINT" },
                    new() { ParamName = "@CheckProcedureCacheFilter", ParamType = "VARCHAR" },
                    new() { ParamName = "@CheckServerInfo", ParamType = "TINYINT" },
                    new() { ParamName = "@SkipChecksServer", ParamType = "NVARCHAR" },
                    new() { ParamName = "@SkipChecksDatabase", ParamType = "NVARCHAR" },
                    new() { ParamName = "@SkipChecksSchema", ParamType = "NVARCHAR" },
                    new() { ParamName = "@SkipChecksTable", ParamType = "NVARCHAR" },
                    new() { ParamName = "@IgnorePrioritiesBelow", ParamType = "INT" },
                    new() { ParamName = "@IgnorePrioritiesAbove", ParamType = "INT" },
                    new() { ParamName = "@OutputXMLasNVARCHAR", ParamType = "TINYINT" },
                    new() { ParamName = "@SummaryMode", ParamType = "TINYINT" },
                    new() { ParamName = "@BringThePain", ParamType = "TINYINT" },
                    new() { ParamName = "@UsualDBOwner", ParamType = "SYSNAME" },
                    new() { ParamName = "@SkipBlockingChecks", ParamType = "TINYINT" },
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
                                "QueryPlan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "QueryPlan"
                                }
                            },
                            {
                                "QueryPlanFiltered",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "QueryPlanFiltered"
                                }
                            },
                            {
                                "URL",
                                new UrlLinkColumnInfo()
                                {
                                    TargetColumn = "URL"
                                }
                            },
                        }
                    }
                }
            },
        };

        public static DirectExecutionReport sp_BlitzWho = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_BlitzWho.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_BlitzWho.ToString(),
            URL = FirstResponderKitUrl,
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
                                "live_query_plan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "live_query_plan"
                                }
                            },
                            {
                                "query_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "query_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "outer_command",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "outer_command",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "fix_parameter_sniffing",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "fix_parameter_sniffing",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            }
                        }
                    }
                }
            },
        };

        public static DirectExecutionReport sp_BlitzIndex = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_BlitzIndex.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_BlitzIndex.ToString(),
            URL = FirstResponderKitUrl,
            Description = "SQL Server Index Analysis Stored Procedure",
            DatabaseNameParameter = "@DatabaseName",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@DatabaseName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@SchemaName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@TableName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@Mode", ParamType = "TINYINT" },
                    new() { ParamName = "@Filter", ParamType = "TINYINT" },
                    new() { ParamName = "@SkipPartitions", ParamType = "BIT" },
                    new() { ParamName = "@SkipStatistics", ParamType = "BIT" },
                    new() { ParamName = "@GetAllDatabases", ParamType = "BIT" },
                    new() { ParamName = "@ShowColumnstoreOnly", ParamType = "BIT" },
                    new() { ParamName = "@BringThePain", ParamType = "BIT" },
                    new() { ParamName = "@IgnoreDatabases", ParamType = "NVARCHAR" },
                    new() { ParamName = "@ThresholdMB", ParamType = "INT" },
                    new() { ParamName = "@IncludeInactiveIndexes", ParamType = "BIT" },
                    new() { ParamName = "@ShowAllMissingIndexRequests", ParamType = "BIT" },
                    new() { ParamName = "@ShowPartitionRanges", ParamType = "BIT" },
                    new() { ParamName = "@SortOrder", ParamType = "NVARCHAR" },
                    new() { ParamName = "@SortDirection", ParamType = "NVARCHAR" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@Mode",
                    Name = "Mode",
                    DefaultValue = 0,
                    PickerItems = new()
                    {
                        { 0, "0 - Diagnose" },
                        { 1, "1 - Summarize" },
                        { 2, "2 - Index Usage Detail" },
                        { 3, "3 - Missing Index Detail" },
                        { 4, "4 - Diagnose Details" }
                    }
                },
                new()
                {
                    ParameterName = "@SkipPartitions",
                    Name = "Skip Partitions",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@SkipStatistics",
                    Name = "Skip Statistics",
                    DefaultValue = true,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@GetAllDatabases",
                    Name = "Get All Databases",
                    DefaultValue = true,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@ShowColumnstoreOnly",
                    Name = "Show Columnstore Only",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@BringThePain",
                    Name = "Bring The Pain",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@ThresholdMB",
                    Name = "Threshold",
                    DefaultValue = 256,
                    PickerItems = new()
                    {
                        { 0, "0" },
                        { 128, "128 MB" },
                        { 256, "256 MB" },
                        { 512, "512 MB" },
                        { 1024, "1 GB" },
                        { 2048, "2 GB" },
                        { 4096, "4 GB" },
                        { 8192, "8 GB" },
                        { 16384, "16 GB" },
                        { 32768, "32 GB" },
                        { 65536, "64 GB" },
                        { 131072, "128 GB" },
                    }
                },
                new()
                {
                    ParameterName = "@IncludeInactiveIndexes",
                    Name = "Include Inactive Indexes",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@ShowAllMissingIndexRequests",
                    Name = "Show All Missing Index Requests",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes" },
                    }
                },
                new()
                {
                    ParameterName = "@ShowPartitionRanges",
                    Name = "Show Partition Ranges",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { false, "No" },
                        { true, "Yes (Add partition range values to columnstore visualization)" },
                    }
                },
                new()
                {
                    ParameterName = "@SortOrder",
                    Name = "Sort Order (For Mode=2)",
                    DefaultValue = null,
                    PickerItems = new()
                    {
                        { "rows", "rows" },
                        { "reserved_mb", "reserved_mb" },
                        { "size", "size" },
                        { "reserved_lob_mb", "reserved_lob_mb" },
                        { "lob", "lob" },
                        { "total_row_lock_wait_in_ms", "total_row_lock_wait_in_ms" },
                        { "total_page_lock_wait_in_ms", "total_page_lock_wait_in_ms" },
                        { "lock_time", "lock_time" },
                        { "total_reads", "total_reads" },
                        { "reads", "reads" },
                        { "user_updates", "user_updates" },
                        { "writes", "writes" },
                        { "reads_per_write", "reads_per_write" },
                        { "ratio", "ratio" },
                        { "forward_fetches", "forward_fetches" },
                        { "fetches", "fetches" },
                        { "create_date", "create_date" },
                        { "modify_date", "modify_date" },
                    }
                },
                new()
                {
                    ParameterName = "@SortDirection",
                    Name = "Sort Direction (For Mode=2)",
                    DefaultValue = "DESC",
                    PickerItems = new()
                    {
                        { "DESC", "DESC" },
                        { "ASC", "ASC" },
                    }
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "Create TSQL",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "Create TSQL",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "More Info",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "More Info",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "URL",
                                new UrlLinkColumnInfo()
                                {
                                    TargetColumn = "URL",
                                }
                            },
                        }
                    }
                }
            },
        };

        public static DirectExecutionReport sp_LogHunter = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_LogHunter.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_LogHunter.ToString(),
            URL = ErikDarlingUrl,
            Description = "Search ErrorLog",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@days_back", ParamType = "INT" },
                    new() { ParamName = "@custom_message", ParamType = "NVARCHAR" },
                    new() { ParamName = "@custom_message_only", ParamType = "BIT" },
                    new() { ParamName = "@first_log_only", ParamType = "BIT" },
                    new() { ParamName = "@start_date", ParamType = "DATETIME" },
                    new() { ParamName = "@end_date", ParamType = "DATETIME" },
                    new() { ParamName = "@language_id", ParamType = "INT" },
                }
            }
        };

        public static DirectExecutionReport sp_BlitzCache = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_BlitzCache.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_BlitzCache.ToString(),
            URL = FirstResponderKitUrl,
            Description = "List the most resource-intensive queries from the plan cache",
            DatabaseNameParameter = "@DatabaseName",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@Top", ParamType = "INT" },
                    new() { ParamName = "@SortOrder", ParamType = "VARCHAR" },
                    new() { ParamName = "@UseTriggersAnyway", ParamType = "BIT" },
                    new() { ParamName = "@ExportToExcel", ParamType = "BIT" },
                    new() { ParamName = "@ExpertMode", ParamType = "TINYINT" },
                    new() { ParamName = "@ConfigurationDatabaseName", ParamType = "NVARCHAR(128)" },
                    new() { ParamName = "@ConfigurationSchemaName", ParamType = "NVARCHAR(258)" },
                    new() { ParamName = "@ConfigurationTableName", ParamType = "NVARCHAR(258)" },
                    new() { ParamName = "@DurationFilter", ParamType = "DECIMAL" },
                    new() { ParamName = "@HideSummary", ParamType = "BIT" },
                    new() { ParamName = "@IgnoreSystemDBs", ParamType = "BIT" },
                    new() { ParamName = "@OnlyQueryHashes", ParamType = "VARCHAR" },
                    new() { ParamName = "@IgnoreQueryHashes", ParamType = "VARCHAR" },
                    new() { ParamName = "@OnlySqlHandles", ParamType = "VARCHAR" },
                    new() { ParamName = "@IgnoreSqlHandles", ParamType = "VARCHAR" },
                    new() { ParamName = "@QueryFilter", ParamType = "VARCHAR" },
                    new() { ParamName = "@DatabaseName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@StoredProcName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@SlowlySearchPlansFor", ParamType = "NVARCHAR" },
                    new() { ParamName = "@Reanalyze", ParamType = "BIT" },
                    new() { ParamName = "@SkipAnalysis", ParamType = "BIT" },
                    new() { ParamName = "@BringThePain", ParamType = "BIT" },
                    new() { ParamName = "@MinimumExecutionCount", ParamType = "INT" },
                    new() { ParamName = "@MinutesBack", ParamType = "INT" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@Top",
                    Name = "Top",
                    DefaultValue = null,
                    PickerItems = new()
                    {
                        { 5, "5" },
                        { 10, "10" },
                        { 15, "15" },
                        { 20, "20" },
                        { 30, "30" },
                        { 50, "50" },
                        { 100, "100" },
                    }
                },
                new()
                {
                    ParameterName = "@SortOrder",
                    Name = "Sort",
                    DefaultValue = "CPU",
                    PickerItems = new()
                    {
                        { "CPU", "CPU" },
                        { "Average CPU", "Average CPU" },
                        { "Reads", "Reads" },
                        { "Average Reads", "Average Reads" },
                        { "Writes", "Writes" },
                        { "Average Writes", "Average Writes" },
                        { "Duration", "Duration" },
                        { "Average Duration", "Average Duration" },
                        { "Executions", "Executions" },
                        { "Average Executions", "Average Executions" },
                        { "Recent Compilations", "Recent Compilations" },
                        { "Average Recent Compilations", "Average Recent Compilations" },
                        { "Memory Grant", "Memory Grant" },
                        { "Average Memory Grant", "Average Memory Grant" },
                        { "Unused Grant", "Unused Grant" },
                        { "Average Unused Grant", "Average Unused Grant" },
                        { "Spills", "Spills" },
                        { "Average Spills", "Average Spills" },
                        { "Query Hash", "Query Hash" },
                        { "Average Query Hash", "Average Query Hash" },
                        { "Duplicate", "Duplicate" },
                        { "Average Duplicate", "Average Duplicate" },
                        { "Executions per minute", "Executions per minute" }
                    }
                },
                new()
                {
                    ParameterName = "@UseTriggersAnyway",
                    Name = "Use Triggers Anyway",
                    DefaultValue = null,
                    PickerItems = new()
                    {
                        { true, "Yes" },
                        { false, "No" },
                    }
                },
                new()
                {
                    ParameterName = "@ExpertMode",
                    Name = "Expert Mode",
                    DefaultValue = 0,
                    PickerItems = new()
                    {
                        { 0, "0 - Default" },
                        { 1, "1 - Expert" },
                    }
                },
                new()
                {
                    ParameterName = "@HideSummary",
                    Name = "Hide Summary",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { true, "Yes" },
                        { false, "No" },
                    }
                },
                new()
                {
                    ParameterName = "@IgnoreSystemDBs",
                    Name = "Ignore System DBs",
                    DefaultValue = true,
                    PickerItems = new()
                    {
                        { true, "Yes" },
                        { false, "No" },
                    }
                },
                new()
                {
                    ParameterName = "@SkipAnalysis",
                    Name = "Skip Analysis",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { true, "Yes" },
                        { false, "No" },
                    }
                },
                new()
                {
                    ParameterName = "@BringThePain",
                    Name = "Bring The Pain",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { true, "Yes" },
                        { false, "No" },
                    }
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Main",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "Query Text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "Query Text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "SET Options",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "SET Options",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "Missing Indexes",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "Missing Indexes",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "Implicit Conversion Info",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "Implicit Conversion Info",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "Cached Execution Parameters",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "Cached Execution Parameters",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "Remove Plan Handle From Cache",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "Remove Plan Handle From Cache",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "Query Plan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "Query Plan",
                                }
                            },
                        }
                    }
                },
                {
                    1, new CustomReportResult()
                    {
                        ResultName = "Summary",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "URL",
                                new UrlLinkColumnInfo()
                                {
                                    TargetColumn = "URL",
                                }
                            },
                        }
                    }
                }
            },
        };

        public static DirectExecutionReport sp_BlitzLock = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_BlitzLock.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_BlitzLock.ToString(),
            URL = FirstResponderKitUrl,
            Description = "SQL Server Deadlock Analysis Stored Procedure",
            DatabaseNameParameter = "@DatabaseName",
            Params = new Params()
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@DatabaseName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@StartDate", ParamType = "DATETIME" },
                    new() { ParamName = "@EndDate", ParamType = "DATETIME" },
                    new() { ParamName = "@ObjectName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@StoredProcName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@AppName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@HostName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@LoginName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@EventSessionName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@TargetSessionType", ParamType = "NVARCHAR" },
                    new() { ParamName = "@VictimsOnly", ParamType = "BIT" },
                    new() { ParamName = "@DeadlockType", ParamType = "NVARCHAR" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@DeadlockType",
                    Name = "Deadlock Type",
                    DefaultValue = string.Empty,
                    PickerItems = new()
                    {
                        { "Regular Deadlock", "Regular Deadlock" },
                        { "Parallel Deadlock", "Parallel Deadlock" },
                        { string.Empty, "Default" },
                    }
                },
                new()
                {
                    ParameterName = "@VictimsOnly",
                    Name = "Victims Only",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { true, "Yes" },
                        { false, "No" },
                    }
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Main",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "query",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "query",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "deadlock_graph",
                                new DeadlockGraphLinkColumnInfo()
                                {
                                    TargetColumn = "deadlock_graph",
                                }
                            },
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "Plans",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "query_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "query_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "query_plan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "query_plan",
                                }
                            },
                        }
                    }
                },
                {
                    2, new CustomReportResult
                    {
                        ResultName = "Findings",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "finding",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "finding",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                        }
                    }
                }
            },
        };

        public static DirectExecutionReport sp_BlitzFirst = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_BlitzFirst.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_BlitzFirst.ToString(),
            URL = FirstResponderKitUrl,
            Description = "This script gives you a prioritized list of why your SQL Server is slow right now",
            Params = new Params()
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@LogMessage", ParamType = "NVARCHAR" },
                    new() { ParamName = "@Help", ParamType = "TINYINT" },
                    new() { ParamName = "@AsOf", ParamType = "DATETIMEOFFSET" },
                    new() { ParamName = "@ExpertMode", ParamType = "TINYINT" },
                    new() { ParamName = "@Seconds", ParamType = "INT" },
                    new() { ParamName = "@OutputType", ParamType = "VARCHAR" },
                    new() { ParamName = "@OutputServerName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@OutputDatabaseName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@OutputSchemaName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@OutputTableName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@OutputTableNameFileStats", ParamType = "NVARCHAR" },
                    new() { ParamName = "@OutputTableNamePerfmonStats", ParamType = "NVARCHAR" },
                    new() { ParamName = "@OutputTableNameWaitStats", ParamType = "NVARCHAR" },
                    new() { ParamName = "@OutputTableNameBlitzCache", ParamType = "NVARCHAR" },
                    new() { ParamName = "@OutputTableNameBlitzWho", ParamType = "NVARCHAR" },
                    new() { ParamName = "@OutputResultSets", ParamType = "NVARCHAR" },
                    new() { ParamName = "@OutputTableRetentionDays", ParamType = "TINYINT" },
                    new() { ParamName = "@OutputXMLasNVARCHAR", ParamType = "TINYINT" },
                    new() { ParamName = "@FilterPlansByDatabase", ParamType = "VARCHAR" },
                    new() { ParamName = "@CheckProcedureCache", ParamType = "TINYINT" },
                    new() { ParamName = "@CheckServerInfo", ParamType = "TINYINT" },
                    new() { ParamName = "@FileLatencyThresholdMS", ParamType = "INT" },
                    new() { ParamName = "@SinceStartup", ParamType = "TINYINT" },
                    new() { ParamName = "@ShowSleepingSPIDs", ParamType = "TINYINT" },
                    new() { ParamName = "@BlitzCacheSkipAnalysis", ParamType = "BIT" },
                    new() { ParamName = "@MemoryGrantThresholdPct", ParamType = "DECIMAL" },
                    new() { ParamName = "@LogMessageCheckID", ParamType = "INT" },
                    new() { ParamName = "@LogMessagePriority", ParamType = "TINYINT" },
                    new() { ParamName = "@LogMessageFindingsGroup", ParamType = "VARCHAR" },
                    new() { ParamName = "@LogMessageFinding", ParamType = "VARCHAR" },
                    new() { ParamName = "@LogMessageURL", ParamType = "VARCHAR" },
                    new() { ParamName = "@LogMessageCheckDate", ParamType = "DATETIMEOFFSET" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@Seconds",
                    Name = "Seconds",
                    DefaultValue = 5,
                    PickerItems = new()
                    {
                        { 5, "5" },
                        { 10, "10" },
                        { 15, "15" },
                        { 20, "20" },
                        { 30, "30" },
                        { 40, "40" },
                        { 60, "60" },
                    }
                },
                new()
                {
                    ParameterName = "@SinceStartup",
                    Name = "Since Startup",
                    DefaultValue = 0,
                    PickerItems = new()
                    {
                        { 1, "Yes" },
                        { 0, "No" },
                    }
                },
                new()
                {
                    ParameterName = "@CheckServerInfo",
                    Name = "Check Server Info",
                    DefaultValue = 1,
                    PickerItems = new()
                    {
                        { 1, "Yes" },
                        { 0, "No" },
                    }
                },
                new()
                {
                    ParameterName = "@ExpertMode",
                    Name = "Expert Mode",
                    DefaultValue = 0,
                    PickerItems = new()
                    {
                        { 1, "Yes" },
                        { 0, "No" },
                    }
                },
                new()
                {
                    ParameterName = "@CheckProcedureCache",
                    Name = "Check Procedure Cache",
                    DefaultValue = 0,
                    PickerItems = new()
                    {
                        { 1, "Yes" },
                        { 0, "No" },
                    }
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Running Queries / Findings",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "URL",
                                new UrlLinkColumnInfo()
                                {
                                    TargetColumn = "URL",
                                }
                            },
                            {
                                "Details",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "Details",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "HowToStopIt",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "HowToStopIt",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "QueryText",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "QueryText",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "query_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "query_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "QueryPlan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "QueryPlan",
                                }
                            },
                            {
                                "query_plan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "query_plan",
                                }
                            },
                            {
                                "live_query_plan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "live_query_plan",
                                }
                            },
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "Findings",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "URL",
                                new UrlLinkColumnInfo()
                                {
                                    TargetColumn = "URL",
                                }
                            },
                            {
                                "Details",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "Details",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "HowToStopIt",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "HowToStopIt",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "QueryText",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "QueryText",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "QueryPlan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "QueryPlan",
                                }
                            },
                        }
                    }
                },
                {
                    2, new CustomReportResult
                    {
                        ResultName = "Waits",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "URL",
                                new UrlLinkColumnInfo()
                                {
                                    TargetColumn = "URL",
                                }
                            }
                        }
                    }
                },
                {
                    3, new CustomReportResult
                    {
                        ResultName = "IO",
                    }
                },
                {
                    4, new CustomReportResult
                    {
                        ResultName = "Perfmon",
                    }
                },
                {
                    5, new CustomReportResult
                    {
                        ResultName = "Plan Cache",
                    }
                },
                {
                    6, new CustomReportResult
                    {
                        ResultName = "Running Queries",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "query_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "query_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "query_plan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "query_plan",
                                }
                            },
                            {
                                "live_query_plan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "live_query_plan",
                                }
                            },
                        }
                    }
                }
            },
        };

        public static DirectExecutionReport sp_BlitzBackups = new()
        {
            ReportName = ProcedureExecutionMessage.CommandNames.sp_BlitzBackups.ToString(),
            URL = FirstResponderKitUrl,
            Description = "Backup Analysis",
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_BlitzBackups.ToString(),
            Params = new Params()
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@HoursBack", ParamType = "INT" },
                    new() { ParamName = "@MSDBName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@AGName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@RestoreSpeedFullMBps", ParamType = "INT" },
                    new() { ParamName = "@RestoreSpeedDiffMBps", ParamType = "INT" },
                    new() { ParamName = "@RestoreSpeedLogMBps", ParamType = "INT" },
                    new() { ParamName = "@WriteBackupsToListenerName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@WriteBackupsToDatabaseName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@WriteBackupsLastHours", ParamType = "INT" },
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "RPO/RTO",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "RPOWorstCaseMoreInfoQuery",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "RPOWorstCaseMoreInfoQuery",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "Backup Stats",
                    }
                },
                {
                    2, new CustomReportResult
                    {
                        ResultName = "Findings",
                    }
                },
            }
        };

        public static DirectExecutionReport sp_HumanEvents = new()
        {
            ReportName = ProcedureExecutionMessage.CommandNames.sp_HumanEvents.ToString(),
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_HumanEvents.ToString(),
            URL = ErikDarlingUrl,
            Description = "Extended events capture",
            CancellationMessageWarning =
                "Cancellation of this report will leave an extended event session running that will require cleanup.",
            DatabaseNameParameter = "@database_name",
            Params = new Params()
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@event_type", ParamType = "NVARCHAR" },
                    new() { ParamName = "@query_duration_ms", ParamType = "INTEGER" },
                    new() { ParamName = "@query_sort_order", ParamType = "NVARCHAR" },
                    new() { ParamName = "@skip_plans", ParamType = "BIT" },
                    new() { ParamName = "@blocking_duration_ms", ParamType = "INTEGER" },
                    new() { ParamName = "@wait_type", ParamType = "NVARCHAR" },
                    new() { ParamName = "@wait_duration_ms", ParamType = "INTEGER" },
                    new() { ParamName = "@client_app_name", ParamType = "NVARCHAR" },
                    new() { ParamName = "@client_hostname", ParamType = "NVARCHAR" },
                    new() { ParamName = "@database_name", ParamType = "NVARCHAR" },
                    new() { ParamName = "@session_id", ParamType = "NVARCHAR" },
                    new() { ParamName = "@sample_divisor", ParamType = "INTEGER" },
                    new() { ParamName = "@username", ParamType = "NVARCHAR" },
                    new() { ParamName = "@object_name", ParamType = "NVARCHAR" },
                    new() { ParamName = "@object_schema", ParamType = "NVARCHAR" },
                    new() { ParamName = "@requested_memory_mb", ParamType = "INTEGER" },
                    new() { ParamName = "@seconds_sample", ParamType = "TINYINT" },
                    new() { ParamName = "@gimme_danger", ParamType = "BIT" },
                    new() { ParamName = "@keep_alive", ParamType = "BIT" },
                    new() { ParamName = "@custom_name", ParamType = "NVARCHAR" },
                    new() { ParamName = "@output_database_name", ParamType = "NVARCHAR" },
                    new() { ParamName = "@output_schema_name", ParamType = "NVARCHAR" },
                    new() { ParamName = "@delete_retention_days", ParamType = "INTEGER" },
                    new() { ParamName = "@cleanup", ParamType = "BIT" },
                    new() { ParamName = "@max_memory_kb", ParamType = "BIGINT" }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@event_type",
                    Name = "Event Type",
                    DefaultValue = "query",
                    PickerItems = new()
                    {
                        { "blocking", "Blocking" },
                        { "query", "Query" },
                        { "waits", "Waits" },
                        { "recompiles", "Recompiles" },
                        { "compiles", "Compiles" }
                    }
                },
                new()
                {
                    ParameterName = "@query_duration_ms",
                    Name = "Query Duration",
                    DefaultValue = 500,
                    PickerItems = new()
                    {
                        { 0, "0" },
                        { 10, "10ms" },
                        { 20, "20ms" },
                        { 50, "50ms" },
                        { 100, "100ms" },
                        { 250, "250ms" },
                        { 500, "500ms" },
                        { 1000, "1 second" },
                        { 2000, "2 seconds" },
                        { 5000, "5 seconds" },
                        { 10000, "10 seconds" },
                        { 20000, "20 seconds" },
                        { 30000, "30 seconds" },
                        { 60000, "60 seconds" },
                    }
                },
                new()
                {
                    ParameterName = "@seconds_sample",
                    Name = "Sample Duration",
                    DefaultValue = 10,
                    PickerItems = new()
                    {
                        { 5, "5 seconds" },
                        { 10, "10 seconds" },
                        { 20, "20 seconds" },
                        { 30, "30 seconds" },
                        { 40, "40 seconds" },
                        { 60, "1 minute" },
                        { 90, "1 minute 30 seconds" },
                        { 120, "2 minutes" },
                    }
                },
                new()
                {
                    ParameterName = "@query_sort_order",
                    Name = "Sort Order (query)",
                    DefaultValue = "cpu",
                    PickerItems = new()
                    {
                        { "cpu", "CPU" },
                        { "avg cpu", "Avg CPU" },
                        { "reads", "Reads" },
                        { "avg reads", "Avg Reads" },
                        { "writes", "Writes" },
                        { "avg writes", "Avg Writes" },
                        { "duration", "Duration" },
                        { "avg duration", "Avg Duration" },
                        { "memory", "Memory" },
                        { "avg memory", "Avg Memory" },
                        { "spills", "Spills" },
                        { "avg spills", "Avg Spills" },
                    }
                },
                new()
                {
                    ParameterName = "@skip_plans",
                    Name = "Skip Plans",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@cleanup",
                    Name = "Cleanup",
                    DefaultValue = true,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@blocking_duration_ms",
                    Name = "Blocking Duration",
                    DefaultValue = 500,
                    PickerItems = new()
                    {
                        { 0, "0" },
                        { 10, "10ms" },
                        { 20, "20ms" },
                        { 50, "50ms" },
                        { 100, "100ms" },
                        { 250, "250ms" },
                        { 500, "500ms" },
                        { 1000, "1 second" },
                        { 2000, "2 seconds" },
                        { 5000, "5 seconds" },
                        { 10000, "10 seconds" },
                        { 20000, "20 seconds" },
                        { 30000, "30 seconds" },
                        { 60000, "60 seconds" },
                    }
                },
                new()
                {
                    ParameterName = "@wait_duration_ms",
                    Name = "Wait Duration",
                    DefaultValue = 10,
                    PickerItems = new()
                    {
                        { 0, "0" },
                        { 10, "10ms" },
                        { 20, "20ms" },
                        { 50, "50ms" },
                        { 100, "100ms" },
                        { 250, "250ms" },
                        { 500, "500ms" },
                        { 1000, "1 second" },
                        { 2000, "2 seconds" },
                        { 5000, "5 seconds" },
                        { 10000, "10 seconds" },
                        { 20000, "20 seconds" },
                        { 30000, "30 seconds" },
                        { 60000, "60 seconds" },
                    }
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "showplan_xml",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "showplan_xml"
                                }
                            },
                            {
                                "query_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "query_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
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
                                "statement_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "statement_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                    }
                },
                {
                    2, new CustomReportResult
                    {
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "query_plan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "showplan_xml"
                                }
                            },
                            {
                                "query_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "query_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "statement_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "statement_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                        }
                    }
                }
            },
        };

        // Result sets can change order, so define the link columns once with links across all results.
        private static CustomReportResult GetPressureDetectorResult(string name) => new()
        {
            ResultName = name,
            LinkColumns = new Dictionary<string, LinkColumnInfo>
            {
                {
                    "tempdb_info",
                    new TextLinkColumnInfo()
                        { TargetColumn = "tempdb_info", TextHandling = CodeEditor.CodeEditorModes.XML }
                },
                {
                    "low_memory",
                    new TextLinkColumnInfo()
                        { TargetColumn = "low_memory", TextHandling = CodeEditor.CodeEditorModes.XML }
                },
                {
                    "cache_memory",
                    new TextLinkColumnInfo()
                        { TargetColumn = "cache_memory", TextHandling = CodeEditor.CodeEditorModes.XML }
                },
                {
                    "max_memory_grant_cap",
                    new TextLinkColumnInfo()
                        { TargetColumn = "max_memory_grant_cap", TextHandling = CodeEditor.CodeEditorModes.XML }
                },
                {
                    "cpu_details_output",
                    new TextLinkColumnInfo()
                        { TargetColumn = "cpu_details_output", TextHandling = CodeEditor.CodeEditorModes.XML }
                },
                {
                    "cpu_utilization_over_threshold",
                    new TextLinkColumnInfo()
                    {
                        TargetColumn = "cpu_utilization_over_threshold", TextHandling = CodeEditor.CodeEditorModes.XML
                    }
                },
                {
                    "query_text",
                    new TextLinkColumnInfo()
                        { TargetColumn = "query_text", TextHandling = CodeEditor.CodeEditorModes.SQL }
                },
                {
                    "query_plan",
                    new QueryPlanLinkColumnInfo() { TargetColumn = "query_plan" }
                },
                {
                    "live_query_plan",
                    new QueryPlanLinkColumnInfo() { TargetColumn = "live_query_plan" }
                },
            }
        };

        public static DirectExecutionReport sp_PressureDetector = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_PressureDetector.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_PressureDetector.ToString(),
            URL = ErikDarlingUrl,
            Description = "Pressure Detector - CPU, Memory etc",
            Params = new Params()
            {
                ParamList = new()
                {
                    new Param { ParamName = "@what_to_check", ParamType = "VARCHAR" },
                    new Param { ParamName = "@skip_queries", ParamType = "BIT" },
                    new Param { ParamName = "@skip_plan_xml", ParamType = "BIT" },
                    new Param { ParamName = "@minimum_disk_latency_ms", ParamType = "SMALLINT" },
                    new Param { ParamName = "@cpu_utilization_threshold", ParamType = "SMALLINT" },
                    new Param { ParamName = "@skip_waits", ParamType = "BIT" },
                    new Param { ParamName = "@skip_perfmon", ParamType = "BIT" },
                    new Param { ParamName = "@sample_seconds", ParamType = "TINYINT" },
                    new Param { ParamName = "@help", ParamType = "BIT" }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@what_to_check",
                    Name = "What To Check",
                    DefaultValue = "all",
                    PickerItems = new()
                    {
                        { "all", "All" },
                        { "cpu", "CPU" },
                        { "memory", "Memory" }
                    },
                },
                new()
                {
                    ParameterName = "@skip_queries",
                    Name = "Skip Queries",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@skip_plan_xml",
                    Name = "Skip Query Plan",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@skip_waits",
                    Name = "Skip Waits",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@skip_perfmon",
                    Name = "Skip Perfmon",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@sample_seconds",
                    Name = "Sample Seconds",
                    DefaultValue = false,
                    PickerItems = new()
                    {
                        { 5, "5 seconds" },
                        { 10, "10 seconds" },
                        { 20, "20 seconds" },
                        { 30, "30 seconds" },
                        { 40, "40 seconds" },
                        { 60, "1 minute" },
                        { 90, "1 minute 30 seconds" },
                        { 120, "2 minutes" },
                    }
                },
                new()
                {
                    ParameterName = "@minimum_disk_latency_ms",
                    Name = "Minimum Disk Latency",
                    DefaultValue = 100,
                    PickerItems = new()
                    {
                        { 0, "0" },
                        { 10, "10ms" },
                        { 20, "20ms" },
                        { 50, "50ms" },
                        { 100, "100ms" },
                        { 250, "250ms" },
                        { 500, "500ms" },
                        { 1000, "1 second" },
                        { 2000, "2 seconds" },
                        { 5000, "5 seconds" },
                        { 10000, "10 seconds" },
                        { 20000, "20 seconds" },
                        { 30000, "30 seconds" },
                        { 60000, "60 seconds" },
                    }
                },
                new()
                {
                    ParameterName = "@cpu_utilization_threshold",
                    Name = "High CPU Utilization Threshold",
                    DefaultValue = 50,
                    PickerItems = new()
                    {
                        { 0, "0" },
                        { 10, "10" },
                        { 20, "20" },
                        { 30, "30" },
                        { 40, "40" },
                        { 50, "50" },
                        { 60, "60" },
                        { 70, "70" },
                        { 80, "80" },
                        { 90, "90" },
                        { 100, "100" },
                    }
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                { 0, GetPressureDetectorResult("1") },
                { 1, GetPressureDetectorResult("2") },
                { 2, GetPressureDetectorResult("3") },
                { 3, GetPressureDetectorResult("4") },
                { 4, GetPressureDetectorResult("5") },
                { 5, GetPressureDetectorResult("6") },
                { 6, GetPressureDetectorResult("7") },
                { 7, GetPressureDetectorResult("8") },
                { 8, GetPressureDetectorResult("9") },
                { 9, GetPressureDetectorResult("10") },
                { 10, GetPressureDetectorResult("11") },
                { 11, GetPressureDetectorResult("12") },
                { 12, GetPressureDetectorResult("13") }
            },
        };

        // Result sets can change order, so define the link columns once with links across all results.
        private static CustomReportResult GetHealthParserResult(string name) => new()
        {
            ResultName = name,
            LinkColumns = new Dictionary<string, LinkColumnInfo>
            {
                {
                    "query_text",
                    new TextLinkColumnInfo()
                        { TargetColumn = "query_text", TextHandling = CodeEditor.CodeEditorModes.SQL }
                },
                {
                    "query_plan",
                    new QueryPlanLinkColumnInfo() { TargetColumn = "query_plan" }
                },
                {
                    "blocked_process_report",
                    new TextLinkColumnInfo()
                        { TargetColumn = "blocked_process_report", TextHandling = CodeEditor.CodeEditorModes.XML }
                },
                {
                    "deadlock_resources",
                    new TextLinkColumnInfo()
                        { TargetColumn = "deadlock_resources", TextHandling = CodeEditor.CodeEditorModes.XML }
                },
                {
                    "deadlock_graph",
                    new DeadlockGraphLinkColumnInfo() { TargetColumn = "deadlock_graph" }
                },
            }
        };

        public static DirectExecutionReport sp_HealthParser = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_HealthParser.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_HealthParser.ToString(),
            URL = ErikDarlingUrl,
            Description = "Returns data from system health extended event.",
            DatabaseNameParameter = "@database_name",
            Params = new Params()
            {
                ParamList = new()
                {
                    new Param { ParamName = "@what_to_check", ParamType = "VARCHAR" },
                    new Param { ParamName = "@start_date", ParamType = "DATETIME" },
                    new Param { ParamName = "@end_date", ParamType = "DATETIME" },
                    new Param { ParamName = "@warnings_only", ParamType = "BIT" },
                    new Param { ParamName = "@database_name", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@wait_duration_ms", ParamType = "BIGINT" },
                    new Param { ParamName = "@wait_round_interval_minutes", ParamType = "BIGINT" },
                    new Param { ParamName = "@skip_locks", ParamType = "BIT" },
                    new Param { ParamName = "@pending_task_threshold", ParamType = "INT" }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@what_to_check",
                    Name = "What To Check",
                    DefaultValue = "all",
                    PickerItems = new()
                    {
                        { "all", "All" },
                        { "waits", "Waits" },
                        { "disk", "Disk" },
                        { "cpu", "CPU" },
                        { "memory", "Memory" },
                        { "system", "System" },
                        { "locking", "Locking" },
                    },
                },
                new()
                {
                    ParameterName = "@warnings_only",
                    Name = "Warnings Only",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@wait_duration_ms",
                    Name = "Wait Duration",
                    DefaultValue = 10,
                    PickerItems = new()
                    {
                        { 0, "0" },
                        { 10, "10ms" },
                        { 20, "20ms" },
                        { 50, "50ms" },
                        { 100, "100ms" },
                        { 250, "250ms" },
                        { 500, "500ms" },
                        { 1000, "1 second" },
                        { 2000, "2 seconds" },
                        { 5000, "5 seconds" },
                        { 10000, "10 seconds" },
                        { 20000, "20 seconds" },
                        { 30000, "30 seconds" },
                        { 60000, "60 seconds" },
                    }
                },
                new()
                {
                    ParameterName = "@skip_locks",
                    Name = "Skip Locks",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                { 0, GetHealthParserResult("1") },
                { 1, GetHealthParserResult("2") },
                { 2, GetHealthParserResult("3") },
                { 3, GetHealthParserResult("4") },
                { 4, GetHealthParserResult("5") },
                { 5, GetHealthParserResult("6") },
                { 6, GetHealthParserResult("7") },
                { 7, GetHealthParserResult("8") },
                { 8, GetHealthParserResult("9") },
                { 9, GetHealthParserResult("10") },
                { 10, GetHealthParserResult("11") },
                { 11, GetHealthParserResult("12") }
            },
        };

        public static DirectExecutionReport sp_QuickieStore = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_QuickieStore.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_QuickieStore.ToString(),
            URL = ErikDarlingUrl,
            Description = "Query store analysis",
            DatabaseNameParameter = "@database_name",
            Params = new Params()
            {
                ParamList = new()
                {
                    new Param { ParamName = "@database_name", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@sort_order", ParamType = "VARCHAR" },
                    new Param { ParamName = "@top", ParamType = "BIGINT" },
                    new Param { ParamName = "@start_date", ParamType = "DATETIMEOFFSET" },
                    new Param { ParamName = "@end_date", ParamType = "DATETIMEOFFSET" },
                    new Param { ParamName = "@timezone", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@execution_count", ParamType = "BIGINT" },
                    new Param { ParamName = "@duration_ms", ParamType = "BIGINT" },
                    new Param { ParamName = "@execution_type_desc", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@procedure_schema", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@procedure_name", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@include_plan_ids", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@include_query_ids", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@include_query_hashes", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@include_plan_hashes", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@include_sql_handles", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@ignore_plan_ids", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@ignore_query_ids", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@ignore_query_hashes", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@ignore_plan_hashes", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@ignore_sql_handles", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@query_text_search", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@query_text_search_not", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@escape_brackets", ParamType = "BIT" },
                    new Param { ParamName = "@escape_character", ParamType = "NCHAR" },
                    new Param { ParamName = "@only_queries_with_hints", ParamType = "BIT" },
                    new Param { ParamName = "@only_queries_with_feedback", ParamType = "BIT" },
                    new Param { ParamName = "@only_queries_with_variants", ParamType = "BIT" },
                    new Param { ParamName = "@only_queries_with_forced_plans", ParamType = "BIT" },
                    new Param { ParamName = "@only_queries_with_forced_plan_failures", ParamType = "BIT" },
                    new Param { ParamName = "@wait_filter", ParamType = "VARCHAR" },
                    new Param { ParamName = "@query_type", ParamType = "VARCHAR" },
                    new Param { ParamName = "@expert_mode", ParamType = "BIT" },
                    new Param { ParamName = "@hide_help_table", ParamType = "BIT" },
                    new Param { ParamName = "@format_output", ParamType = "BIT" },
                    new Param { ParamName = "@get_all_databases", ParamType = "BIT" },
                    new Param { ParamName = "@workdays", ParamType = "BIT" },
                    new Param { ParamName = "@work_start", ParamType = "TIME" },
                    new Param { ParamName = "@work_end", ParamType = "TIME" },
                    new Param { ParamName = "@help", ParamType = "BIT" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@execution_type_desc",
                    Name = "Execution Type",
                    PickerItems = new()
                    {
                        { "Aborted", "Aborted" },
                        { "Exception", "Exception" },
                        { "Regular", "Regular" },
                        { "", "ALL" },
                    }
                },
                new()
                {
                    ParameterName = "@escape_brackets",
                    Name = "Escape Brackets",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@only_queries_with_hints",
                    Name = "Only Queries With Hints",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@only_queries_with_feedback",
                    Name = "Only Queries With Feedback",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@only_queries_with_variants",
                    Name = "Only Queries With Variants",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@only_queries_with_forced_plans",
                    Name = "Only Queries With Forced Plans",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@only_queries_with_forced_plan_failures",
                    Name = "Only Queries With Plan Failures",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@expert_mode",
                    Name = "Expert Mode",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@hide_help_table",
                    Name = "Hide Help Table",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@format_output",
                    Name = "Format Output",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@get_all_databases",
                    Name = "Get All Databases",
                    DefaultValue = true,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@workdays",
                    Name = "Workdays",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
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
                                "query_sql_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "query_sql_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                        }
                    }
                },
            }
        };

        public static DirectExecutionReport sp_HumanEventsBlockViewer = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_HumanEventsBlockViewer.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_HumanEventsBlockViewer.ToString(),
            URL = ErikDarlingUrl,
            Description = "Procedure for parsing sqlserver.blocked_process_report extended event",
            DatabaseNameParameter = "@database_name",
            Params = new Params()
            {
                ParamList = new()
                {
                    new Param { ParamName = "@session_name", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@target_type", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@start_date", ParamType = "DATETIME2" },
                    new Param { ParamName = "@end_date", ParamType = "DATETIME2" },
                    new Param { ParamName = "@database_name", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@object_name", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@help", ParamType = "BIT" }
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Blocked Process Report",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "query_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "query_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "blocked_process_report1",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "blocked_process_report1",
                                    TextHandling = CodeEditor.CodeEditorModes.XML
                                }
                            },
                            {
                                "blocked_process_report",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "blocked_process_report1",
                                    TextHandling = CodeEditor.CodeEditorModes.XML
                                }
                            },
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "Plans",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "query_text",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "query_text",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "query_plan",
                                new QueryPlanLinkColumnInfo()
                                {
                                    TargetColumn = "query_plan"
                                }
                            },
                        }
                    }
                },
                {
                    2, new CustomReportResult
                    {
                        ResultName = "Findings"
                    }
                },
            }
        };

        public static DirectExecutionReport sp_SrvPermissions = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_SrvPermissions.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_SrvPermissions.ToString(),
            URL = "https://github.com/sqlstudent144/SQL-Server-Scripts/blob/main/sp_SrvPermissions.sql",
            Description = "Server principals,role membership and permissions",
            DatabaseNameParameter = "@DBName",
            Params = new Params()
            {
                ParamList = new()
                {
                    new Param { ParamName = "@Principal", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@Role", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@Type", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@DBName", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@UseLikeSearch", ParamType = "BIT" },
                    new Param { ParamName = "@IncludeMSShipped", ParamType = "BIT" },
                    new Param { ParamName = "@CopyTo", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@Output", ParamType = "NVARCHAR" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@Type",
                    Name = "Type",
                    PickerItems = new()
                    {
                        { "S", "SQL Login" },
                        { "U", "Windows Login" },
                        { "G", "Windows Group" },
                        { "R", "Server Role" },
                        { "C", "Login mapped to a certificate" },
                        { "K", "Login mapped to a asymmetric key" },
                        { "", "ALL" },
                    }
                },
                new()
                {
                    ParameterName = "IncludeMSShipped",
                    Name = "Include MS Shipped",
                    DefaultValue = true,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@Output",
                    Name = "Output",
                    DefaultValue = "Default",
                    PickerItems = new()
                    {
                        { "Default", "Default" },
                        { "CreateOnly", "Create Only" },
                        { "DropOnly", "DropOnly" },
                        { "ScriptOnly", "Scripts Only" },
                        { "Report", "Report" },
                    }
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "1",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "DropScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "DropScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "CreateScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "CreateScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            }
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "2",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "DropScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "DropScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "AddScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "AddScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            }
                        }
                    }
                },
                {
                    2, new CustomReportResult
                    {
                        ResultName = "3",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "RevokeScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "DropScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "GrantScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "AddScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            }
                        }
                    }
                },
            }
        };

        public static DirectExecutionReport sp_DBPermissions = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommandNames.sp_DBPermissions.ToString(),
            ReportName = ProcedureExecutionMessage.CommandNames.sp_DBPermissions.ToString(),
            URL = "https://github.com/sqlstudent144/SQL-Server-Scripts/blob/main/sp_DBPermissions.sql",
            Description = "Database principals,role membership and permissions",
            DatabaseNameParameter = "@DBName",
            Params = new Params()
            {
                ParamList = new()
                {
                    new Param { ParamName = "@Principal", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@Role", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@Type", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@DBName", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@ObjectName", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@Permission", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@LoginName", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@UseLikeSearch", ParamType = "BIT" },
                    new Param { ParamName = "@IncludeMSShipped", ParamType = "BIT" },
                    new Param { ParamName = "@CopyTo", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@ShowOrphans", ParamType = "BIT" },
                    new Param { ParamName = "@Output", ParamType = "NVARCHAR" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@Type",
                    Name = "Type",
                    PickerItems = new()
                    {
                        { "S", "SQL Login" },
                        { "U", "Windows Login" },
                        { "G", "Windows Group" },
                        { "R", "Server Role" },
                        { "C", "Login mapped to a certificate" },
                        { "K", "Login mapped to a asymmetric key" },
                        { "", "ALL" },
                    }
                },
                new()
                {
                    ParameterName = "IncludeMSShipped",
                    Name = "Include MS Shipped",
                    DefaultValue = true,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "ShowOrphans",
                    Name = "Show Orphans",
                    DefaultValue = false,
                    PickerItems = BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@Output",
                    Name = "Output",
                    DefaultValue = "Default",
                    PickerItems = new()
                    {
                        { "Default", "Default" },
                        { "CreateOnly", "Create Only" },
                        { "DropOnly", "DropOnly" },
                        { "ScriptOnly", "Scripts Only" },
                        { "Report", "Report" },
                    }
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "1",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "DropScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "DropScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "CreateScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "CreateScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            }
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "2",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "DropScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "DropScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "AddScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "AddScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            }
                        }
                    }
                },
                {
                    2, new CustomReportResult
                    {
                        ResultName = "3",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "RevokeScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "DropScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "GrantScript",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "AddScript",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            }
                        }
                    }
                },
            }
        };

        public static List<DirectExecutionReport> CommunityToolsList = new()
        {
            sp_LogHunter,
            sp_WhoIsActive,
            sp_Blitz,
            sp_BlitzWho,
            sp_BlitzIndex,
            sp_BlitzCache,
            sp_BlitzLock,
            sp_BlitzFirst,
            sp_BlitzBackups,
            sp_HumanEvents,
            sp_PressureDetector,
            sp_HealthParser,
            sp_QuickieStore,
            sp_HumanEventsBlockViewer,
            sp_SrvPermissions,
            sp_DBPermissions
        };

        public static List<DirectExecutionReport> DatabaseLevelCommunityTools =>
            CommunityToolsList.Where(x => x.DatabaseNameParameter != null).ToList();
    }
}