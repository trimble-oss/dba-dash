using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_HumanEventsBlockViewer
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_HumanEventsBlockViewer.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_HumanEventsBlockViewer.ToString(),
            URL = CommunityTools.ErikDarlingUrl,
            Description = "Procedure for parsing sqlserver.blocked_process_report extended event",
            DatabaseNameParameter = "@database_name",
            ObjectParameterName = "@object_name",
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
    }
}