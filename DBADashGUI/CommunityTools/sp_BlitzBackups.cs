using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_BlitzBackups
    {
        public static DirectExecutionReport Instance = new()
        {
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzBackups.ToString(),
            URL = CommunityTools.FirstResponderKitUrl,
            Description = "Backup Analysis",
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_BlitzBackups.ToString(),
            Params = new Params()
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@HoursBack", ParamType = "INT" },
                    new() { ParamName = "@MSDBName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@AGName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@RestoreSpeedFullMBps", ParamType = "INT" },
                    new() { ParamName = "@RestoreSpeedDiffMBps", ParamType = "INT" },
                    new() { ParamName = "@RestoreSpeedLogMBps", ParamType = "INT" },
                    new() { ParamName = "@WriteBackupsToListenerName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@WriteBackupsToDatabaseName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@WriteBackupsLastHours", ParamType = "INT" },
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "RPO/RTO",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            {
                                "RPOWorstCaseMoreInfoQuery",
                                new ColumnMetadata
                                {
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "RPOWorstCaseMoreInfoQuery",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    }
                                }
                            },
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "Backup Stats",
                    }
                },
                {
                    2, new CustomReportResult
                    {
                        ResultName = "Findings",
                    }
                },
            }
        };
    }
}