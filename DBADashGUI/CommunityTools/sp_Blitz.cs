using DBADash.Messaging;
using DBADashGUI.CustomReports;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_Blitz
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_Blitz.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_Blitz.ToString(),
            URL = CommunityTools.FirstResponderKitUrl,
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
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            {
                                "QueryPlan",
                                new ColumnMetadata
                                {
                                    Link = new QueryPlanLinkColumnInfo
                                    {
                                        TargetColumn = "QueryPlan"
                                    }
                                }
                            },
                            {
                                "QueryPlanFiltered",
                                new ColumnMetadata
                                {
                                    Link = new QueryPlanLinkColumnInfo
                                    {
                                        TargetColumn = "QueryPlanFiltered"
                                    }
                                }
                            },
                            {
                                "URL",
                                new ColumnMetadata
                                {
                                    Link = new UrlLinkColumnInfo
                                    {
                                        TargetColumn = "URL"
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