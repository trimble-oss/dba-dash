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
                                Alias = "Snapshot Date",
                                Description = "Date and time of the collection"
                            }},
                            { "ObjectName", new ColumnMetadata {
                                Alias = "Object Name",
                                Description = "Table name"
                            }},
                            { "SchemaName", new ColumnMetadata {
                                Alias = "Schema Name",
                                Description = "Table schema name. e.g. dbo"
                            }},
                            { "Reserved_KB", new ColumnMetadata {
                                Alias = "Reserved KB",
                                FormatString = "N0",
                                Description = "Total amount of space reserved in KB for the table"
                            }},
                            { "Used_KB", new ColumnMetadata {
                                Alias = "Used KB",
                                FormatString = "N0",
                                Description = "Total amount of space used in KB for the table"
                            }},
                            { "Data_KB", new ColumnMetadata {
                                Alias = "Data KB",
                                FormatString = "N0",
                                Description = "Total amount of space used by data pages in KB for table data"
                            }},
                            { "Index_KB", new ColumnMetadata {
                                Alias = "Index KB",
                                FormatString = "N0",
                                Description = "Total amount of space used by index pages in KB for table indexes"
                            }},
                            { "New_Rows", new ColumnMetadata {
                                Alias = "New Rows",
                                FormatString = "N0",
                                Description = "Number of new rows since the previous snapshot"
                            }},
                            { "New_KB", new ColumnMetadata {
                                Alias = "New KB",
                                FormatString = "N0",
                                Description = "Amount of KB added (used) since the previous snapshot"
                            }},
                            { "Rows_Per_Hour", new ColumnMetadata {
                                Alias = "Rows Per Hour",
                                FormatString = "N1",
                                Description = "Rate of new rows per hour since the previous snapshot"
                            }},
                            { "KB_Per_Hour", new ColumnMetadata {
                                Alias = "KB Per Hour",
                                FormatString = "N1",
                                Description = "Rate of KB growth per hour since the previous snapshot"
                            }},
                            { "Rows", new ColumnMetadata {
                                FormatString = "N0",
                                Description = "Total number of rows in the table at the time of the snapshot"
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