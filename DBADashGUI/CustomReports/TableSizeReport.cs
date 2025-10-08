using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    internal class TableSizeReport
    {
        public static SystemReport Instance => new()
        {
            ReportName = "Table Size",
            TriggerCollectionTypes = { DBADash.CollectionType.TableSize.ToString() },
            SchemaName = "dbo",
            ProcedureName = "TableSize_Get",
            QualifiedProcedureName = "dbo.TableSize_Get",
            CanEditReport = false,
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Table Size",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "ObjectID", new ColumnMetadata {
                                Visible = false,
                                Description = "DBA Dash object identifier for the table dbo.DBObjects"
                            }},
                            { "InstanceID", new ColumnMetadata {
                                Visible = false,
                                Description = "DBA Dash instance identifier dbo.Instances"
                            }},
                            { "Instance", new ColumnMetadata(){Description="Name of instance" } },
                            { "DB", new ColumnMetadata(){Description="Name of database" } },
                            { "SchemaName", new ColumnMetadata {
                                Alias = "Schema Name",
                                Description = "Table schema name. e.g. dbo"
                            }},
                            { "ObjectName", new ColumnMetadata {
                                Alias = "Object Name",
                                Link = new DrillDownLinkColumnInfo {
                                    ReportProcedureName = "TableSizeHistory_Get",
                                    ColumnToParameterMap = new Dictionary<string, string> { { "@ObjectID", "ObjectID" } }
                                },
                                Description = "Table name"
                            }},
                            { "Rows", new ColumnMetadata {
                                FormatString = "N0",
                                Description = "Number of rows in the table"
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
                                Description = "Total amount of space used by index pages in KB for the table"
                            }},
                            { "Avg_Rows_Per_Day", new ColumnMetadata {
                                Alias = "Avg Rows Per Day",
                                FormatString = "N0",
                                Description = "Average number of rows added per day.\nCalculated over the last @GrowthDays using the diff between the earliest snapshot for the period and the latest snapshot (Calc Days)"
                            }},
                            { "Avg_KB_Per_Day", new ColumnMetadata {
                                Alias = "Avg KB Per Day",
                                FormatString = "N0",
                                Description = "Average amount of KB added per day (used).\nCalculated over the last @GrowthDays using the diff between the earliest snapshot for the period and the latest snapshot (Calc Days)"
                            }},
                            { "CalcDays", new ColumnMetadata {
                                Alias = "Calc Days",
                                FormatString = "N1",
                                Visible = false,
                                Description = "Number of days between the earliest and latest snapshot used to calculate growth per day"
                            }},
                            { "SnapshotDate", new ColumnMetadata {
                                Alias = "Snapshot Date",
                                Highlighting = new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true },
                                Description = "Date of the latest snapshot for the Table Size collection"
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
                        ParamName = "@InstanceIDs",
                        ParamType = "IDS"
                    },
                    new()
                    {
                        ParamName = "@GrowthDays",
                        ParamType = "INT"
                    },
                    new()
                    {
                        ParamName = "@Top",
                        ParamType = "INT",
                    },
                    new()
                    {
                        ParamName = "@DatabaseID",
                        ParamType = "INT"
                    }
                },
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@GrowthDays",
                    Name = "Growth Days",
                    DefaultValue = "30",
                    PickerItems = new()
                    {
                        { "1", "1 Day" },
                        { "2", "2 Days" },
                        { "7", "7 Days" },
                        { "14", "14 Days" },
                        { "30", "30 Days" },
                        { "60", "60 Days" },
                        { "90", "90 Days" },
                        { "365", "365 Days" }
                    },
                    MenuBar = true
                },
                Picker.CreateTopPicker(true)
            }
        };
    }
}