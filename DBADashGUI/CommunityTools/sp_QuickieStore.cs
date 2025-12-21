using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_QuickieStore
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_QuickieStore.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_QuickieStore.ToString(),
            URL = CommunityTools.ErikDarlingUrl,
            Description = "Query store analysis",
            DatabaseNameParameter = "@database_name",
            ObjectParameterName = "@procedure_name",
            SchemaParameterName = "@procedure_schema",
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
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@only_queries_with_hints",
                    Name = "Only Queries With Hints",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@only_queries_with_feedback",
                    Name = "Only Queries With Feedback",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@only_queries_with_variants",
                    Name = "Only Queries With Variants",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@only_queries_with_forced_plans",
                    Name = "Only Queries With Forced Plans",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@only_queries_with_forced_plan_failures",
                    Name = "Only Queries With Plan Failures",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@expert_mode",
                    Name = "Expert Mode",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@hide_help_table",
                    Name = "Hide Help Table",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@format_output",
                    Name = "Format Output",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@get_all_databases",
                    Name = "Get All Databases",
                    DefaultValue = true,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@workdays",
                    Name = "Workdays",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
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
    }
}