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
                        ResultName = "Table Size History",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "SnapshotDate", new ColumnMetadata {
                                Alias = "Snapshot Date"
                            }},
                            { "ObjectName", new ColumnMetadata {
                                Alias = "Object Name"
                            }},
                            { "SchemaName", new ColumnMetadata {
                                Alias = "Schema Name"
                            }},
                            { "Reserved_KB", new ColumnMetadata {
                                Alias = "Reserved KB",
                                FormatString = "N0"
                            }},
                            { "Used_KB", new ColumnMetadata {
                                Alias = "Used KB",
                                FormatString = "N0"
                            }},
                            { "Data_KB", new ColumnMetadata {
                                Alias = "Data KB",
                                FormatString = "N0"
                            }},
                            { "Index_KB", new ColumnMetadata {
                                Alias = "Index KB",
                                FormatString = "N0"
                            }},
                            { "New_Rows", new ColumnMetadata {
                                Alias = "New Rows",
                                FormatString = "N0"
                            }},
                            { "New_KB", new ColumnMetadata {
                                Alias = "New KB",
                                FormatString = "N0"
                            }},
                            { "Rows_Per_Hour", new ColumnMetadata {
                                Alias = "Rows Per Hour",
                                FormatString = "N1"
                            }},
                            { "KB_Per_Hour", new ColumnMetadata {
                                Alias = "KB Per Hour",
                                FormatString = "N1"
                            }},
                            { "Rows", new ColumnMetadata {
                                FormatString = "N0"
                            }}
                        }
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