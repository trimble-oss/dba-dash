using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_HumanEvents
    {
        public static DirectExecutionReport Instance = new()
        {
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_HumanEvents.ToString(),
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_HumanEvents.ToString(),
            URL = CommunityTools.ErikDarlingUrl,
            Description = "Extended events capture",
            CancellationMessageWarning =
             "Cancellation of this report will leave an extended event session running that will require cleanup.",
            DatabaseNameParameter = "@database_name",
            ObjectParameterName = "@object_name",
            SchemaParameterName = "@object_schema",
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
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@cleanup",
                    Name = "Cleanup",
                    DefaultValue = true,
                    PickerItems = CommunityTools.BooleanPickerItems
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
    }
}