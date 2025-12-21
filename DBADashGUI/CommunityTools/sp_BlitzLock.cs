using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_BlitzLock
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzLock.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzLock.ToString(),
            URL = CommunityTools.FirstResponderKitUrl,
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
    }
}