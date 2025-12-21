using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_PressureDetector
    {
        // Result sets can change order, so define the link columns once with links across all results.
        private static CustomReportResult GetPressureDetectorResult(string name) => new()
        {
            ResultName = name,
            Columns = new Dictionary<string, ColumnMetadata>
            {
                {
                    "tempdb_info",
                    new ColumnMetadata
                    {
                        Link = new TextLinkColumnInfo
                        {
                            TargetColumn = "tempdb_info",
                            TextHandling = CodeEditor.CodeEditorModes.XML
                        }
                    }
                },
                {
                    "low_memory",
                    new ColumnMetadata
                    {
                        Link = new TextLinkColumnInfo
                        {
                            TargetColumn = "low_memory",
                            TextHandling = CodeEditor.CodeEditorModes.XML
                        }
                    }
                },
                {
                    "cache_memory",
                    new ColumnMetadata
                    {
                        Link = new TextLinkColumnInfo
                        {
                            TargetColumn = "cache_memory",
                            TextHandling = CodeEditor.CodeEditorModes.XML
                        }
                    }
                },
                {
                    "max_memory_grant_cap",
                    new ColumnMetadata
                    {
                        Link = new TextLinkColumnInfo
                        {
                            TargetColumn = "max_memory_grant_cap",
                            TextHandling = CodeEditor.CodeEditorModes.XML
                        }
                    }
                },
                {
                    "cpu_details_output",
                    new ColumnMetadata
                    {
                        Link = new TextLinkColumnInfo
                        {
                            TargetColumn = "cpu_details_output",
                            TextHandling = CodeEditor.CodeEditorModes.XML
                        }
                    }
                },
                {
                    "cpu_utilization_over_threshold",
                    new ColumnMetadata
                    {
                        Link = new TextLinkColumnInfo
                        {
                            TargetColumn = "cpu_utilization_over_threshold",
                            TextHandling = CodeEditor.CodeEditorModes.XML
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
            }
        };

        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_PressureDetector.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_PressureDetector.ToString(),
            URL = CommunityTools.ErikDarlingUrl,
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
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@skip_plan_xml",
                    Name = "Skip Query Plan",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@skip_waits",
                    Name = "Skip Waits",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@skip_perfmon",
                    Name = "Skip Perfmon",
                    DefaultValue = false,
                    PickerItems = CommunityTools.BooleanPickerItems
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
    }
}