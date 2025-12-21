using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_DBPermissions
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_DBPermissions.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_DBPermissions.ToString(),
            URL = "https://github.com/sqlstudent144/SQL-Server-Scripts/blob/main/sp_DBPermissions.sql",
            Description = "Database principals,role membership and permissions",
            DatabaseNameParameter = "@DBName",
            ObjectParameterName = "@ObjectName",
            Params = new Params()
            {
                ParamList = new()
                {
                    new Param { ParamName = "@Principal", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@Role", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@Type", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@DBName", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@ObjectName", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@Permission", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@LoginName", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@UseLikeSearch", ParamType = "BIT" },
                    new Param { ParamName = "@IncludeMSShipped", ParamType = "BIT" },
                    new Param { ParamName = "@CopyTo", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@ShowOrphans", ParamType = "BIT" },
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
                    ParameterName = "@IncludeMSShipped",
                    Name = "Include MS Shipped",
                    DefaultValue = true,
                    PickerItems = CommunityTools.BooleanPickerItems
                },
                new()
                {
                    ParameterName = "@ShowOrphans",
                    Name = "Show Orphans",
                    DefaultValue = false,
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
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            {
                                "DropScript",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "DropScript",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "CreateScript",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "CreateScript",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            }
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "2",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            {
                                "DropScript",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "DropScript",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "AddScript",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "AddScript",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            }
                        }
                    }
                },
                {
                    2, new CustomReportResult
                    {
                        ResultName = "3",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            {
                                "RevokeScript",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "RevokeScript",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                            {
                                "GrantScript",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "GrantScript",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            }
                        }
                    }
                },
            }
        };
    }
}