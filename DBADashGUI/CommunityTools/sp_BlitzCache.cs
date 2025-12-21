using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_BlitzCache
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzCache.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzCache.ToString(),
            URL = CommunityTools.FirstResponderKitUrl,
            Description = "List the most resource-intensive queries from the plan cache",
            DatabaseNameParameter = "@DatabaseName",
            ObjectParameterName = "@StoredProcName",
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
                    new() { ParamName = "@AI", ParamType = "TINYINT" },
                    new() { ParamName = "@AIModel", ParamType = "VARCHAR" },
                    new() { ParamName = "@AIURL", ParamType = "VARCHAR" },
                    new() { ParamName = "@AIConfig", ParamType = "VARCHAR" },
                    new() { ParamName = "@AICredential", ParamType = "VARCHAR" },
                    new() { ParamName = "@KeepCRLF", ParamType = "BIT" },
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
                        { 1, "1" },
                        { 2, "2" },
                        { 3, "3" },
                        { 4, "4" },
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
                new()
                {
                    ParameterName = "@AI",
                    Name = "AI",
                    DefaultValue = null,
                    PickerItems = new()
                    {
                        { 0, "None"  },
                        { 1, "AI Analysis" },
                        { 2, "Generate prompt" },
                    }
                },
                new()
                {
                    ParameterName = "@KeepCRLF",
                    Name = "Keep CR/LF in text columns",
                    DefaultValue = null,
                    PickerItems = new()
                    {
                        { true, "Yes" },
                        { false, "No" },
                    }
                },
                new()
                {
                    ParameterName = "@AIModel",
                    Name = "AI Model",
                    DefaultValue = null,
                    PickerItems = new()
                    {
                        { "gemini-2.5-flash","gemini-2.5-flash" },
                        { "gemini-2.5-flash-lite","gemini-2.5-flash-lite" },
                        { "gemini-2.5-pro","gemini-2.5-pro" },
                        { "gemini-3-flash-preview","gemini-3-flash-preview" },
                        { "gemini-3-pro-preview","gemini-3-pro-preview" },
                        { "gpt-5-mini", "gpt-5-mini" },
                        { "gpt-5-nano", "gpt-5-nano" },
                        { "gpt-5.1", "gpt-5.1" },
                    }
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Main",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            {
                                "Query Text",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "Query Text",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "SET Options",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "SET Options",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "Missing Indexes",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "Missing Indexes",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "Implicit Conversion Info",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "Implicit Conversion Info",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "Cached Execution Parameters",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "Cached Execution Parameters",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "Remove Plan Handle From Cache",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "Remove Plan Handle From Cache",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "Query Plan",
                                new ColumnMetadata
                                {
                                    Link = new QueryPlanLinkColumnInfo
                                    {
                                        TargetColumn = "Query Plan",
                                    }
                                }
                            },
                            {
                                "AI Prompt",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "AI Prompt",
                                        TextHandling = CodeEditor.CodeEditorModes.XML
                                    }
                                }
                            },
                            {
                                "AI Advice",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "AI Advice",
                                        TextHandling = CodeEditor.CodeEditorModes.XML
                                    }
                                }
                            },
                        }
                    }
                },
                {
                    1, new CustomReportResult()
                    {
                        ResultName = "Summary",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            {
                                "URL",
                                new ColumnMetadata
                                {
                                    Link = new UrlLinkColumnInfo
                                    {
                                        TargetColumn = "URL",
                                    }
                                }
                            },
                        }
                    }
                }
            },
        };
    }
}