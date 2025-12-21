using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_IndexCleanup
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_IndexCleanup.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_IndexCleanup.ToString(),
            URL = CommunityTools.ErikDarlingUrl,
            Description = "This stored procedure helps identify unused and duplicate indexes in your SQL Server databases that could be candidates for removal. It analyzes index usage statistics and can generate scripts for removing unnecessary indexes.",
            DatabaseNameParameter = "@database_name",
            ObjectParameterName = "@table_name",
            Params = new Params()
            {
                ParamList = new()
                {
                    new Param { ParamName = "@database_name", ParamType = "NVARCHAR" },
                    new Param { ParamName ="@schema_name", ParamType = "NVARCHAR"},
                    new Param { ParamName = "@table_name", ParamType = "NVARCHAR" },
                    new Param { ParamName = "@min_reads", ParamType = "BIGINT"},
                    new Param { ParamName ="@min_writes",ParamType = "BIGINT"},
                    new Param {ParamName = "@min_size_gb",@ParamType = "DECIMAL"},
                    new Param {ParamName = "@min_rows",ParamType = "BIGINT"},
                    new Param {ParamName = "@dedupe_only",ParamType = "BIT"},
                    new Param {ParamName = "@get_all_databases",ParamType = "BIT"},
                    new Param {ParamName = "include_databases", ParamType = "NVARCHAR"},
                    new Param {ParamName = "@exclude_databases",@ParamType = "NVARCHAR"},
                    new Param { ParamName = "@help", ParamType = "BIT" }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@get_all_databases",
                    Name = "Get All Databases",
                    DefaultValue = true,
                    PickerItems = CommunityTools.BooleanPickerItems
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "original_index_definition",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "original_index_definition",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                            {
                                "script",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "script",
                                    TextHandling = CodeEditor.CodeEditorModes.SQL
                                }
                            },
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                    }
                },
                {
                    2, new CustomReportResult
                    {
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "original_index_definition",
                                new TextLinkColumnInfo()
                                {
                                    TargetColumn = "original_index_definition",
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