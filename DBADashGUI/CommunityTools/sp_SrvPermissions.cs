using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_SrvPermissions
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_SrvPermissions.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_SrvPermissions.ToString(),
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
                    PickerItems = CommunityTools.BooleanPickerItems
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
    }
}