using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_BlitzFirst
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzFirst.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzFirst.ToString(),
            URL = CommunityTools.FirstResponderKitUrl,
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
                            {
                                "Details",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "Details",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "HowToStopIt",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "HowToStopIt",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "QueryText",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "QueryText",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
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
                                "QueryPlan",
                                new ColumnMetadata
                                {
                                    Link = new QueryPlanLinkColumnInfo
                                    {
                                        TargetColumn = "QueryPlan",
                                    }
                                }
                            },
                            {
                                "query_plan",
                                new ColumnMetadata
                                {
                                    Link = new QueryPlanLinkColumnInfo
                                    {
                                        TargetColumn = "query_plan",
                                    }
                                }
                            },
                            {
                                "live_query_plan",
                                new ColumnMetadata
                                {
                                    Link = new QueryPlanLinkColumnInfo
                                    {
                                        TargetColumn = "live_query_plan",
                                    }
                                }
                            },
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "Findings",
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
                            {
                                "Details",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "Details",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "HowToStopIt",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "HowToStopIt",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "QueryText",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "QueryText",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "QueryPlan",
                                new ColumnMetadata
                                {
                                    Link = new QueryPlanLinkColumnInfo
                                    {
                                        TargetColumn = "QueryPlan",
                                    }
                                }
                            },
                        }
                    }
                },
                {
                    2, new CustomReportResult
                    {
                        ResultName = "Waits",
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
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
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
                                        TargetColumn = "query_plan",
                                    }
                                }
                            },
                            {
                                "live_query_plan",
                                new ColumnMetadata
                                {
                                    Link = new QueryPlanLinkColumnInfo
                                    {
                                        TargetColumn = "live_query_plan",
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