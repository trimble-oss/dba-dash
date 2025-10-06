using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    internal class TableSizeHistoryReport
    {
        public static SystemReport Instance => new()
        {
            ReportName = "Table Size History",
            SchemaName = "dbo",
            ProcedureName = "TableSizeHistory_Get",
            QualifiedProcedureName = "dbo.TableSizeHistory_Get",
            CanEditReport = false,
            CustomReportResults = new Dictionary<int, CustomReportResult>
        {
                    {
                        0, new CustomReportResult
                        {
                            ColumnAlias = new Dictionary<string, string>
                            {
                                { "SnapshotDate", "Snapshot Date" },
                                { "ObjectName", "Object Name" },
                                { "SchemaName", "Schema Name" },
                                { "Reserved_KB", "Reserved KB" },
                                { "Used_KB", "Used KB" },
                                { "Data_KB", "Data KB" },
                                { "Index_KB", "Index KB" },
                                { "New_Rows", "New Rows" },
                                { "New_KB", "New KB" },
                                { "Rows_Per_Hour", "Rows Per Hour" },
                                { "KB_Per_Hour", "KB Per Hour" }
                            },
                            CellFormatString = new Dictionary<string, string>
                            {
                                { "Rows", "N0" },
                                { "Reserved_KB", "N0" },
                                { "Used_KB", "N0" },
                                { "Data_KB", "N0" },
                                { "Index_KB", "N0" },
                                { "New_Rows", "N0" },
                                { "New_KB", "N0" },
                                { "Rows_Per_Hour", "N1" },
                                { "KB_Per_Hour", "N1" }
                            },
                            ResultName = "Table Size History",
                        }
                    }
        },
            Params = new Params
            {
                ParamList = new List<Param>
            {
                        new()
                        {
                            ParamName = "@ObjectID",
                            ParamType = "INT"
                        },
                        new()
                        {
                            ParamName = "@Top",
                            ParamType = "INT"
                        }
            },
            }
        };
    }
}