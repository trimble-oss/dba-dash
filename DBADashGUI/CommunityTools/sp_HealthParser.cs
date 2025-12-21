using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_HealthParser
    {
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

        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_HealthParser.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_HealthParser.ToString(),
            URL = CommunityTools.ErikDarlingUrl,
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
                    PickerItems = CommunityTools.BooleanPickerItems
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
                    PickerItems = CommunityTools.BooleanPickerItems
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
    }
}