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
                                Visible = false
                            }},
                            { "InstanceID", new ColumnMetadata {
                                Visible = false
                            }},
                            { "Instance", new ColumnMetadata() },
                            { "DB", new ColumnMetadata() },
                            { "SchemaName", new ColumnMetadata {
                                Alias = "Schema Name"
                            }},
                            { "ObjectName", new ColumnMetadata {
                                Alias = "Object Name",
                                Link = new DrillDownLinkColumnInfo {
                                    ReportProcedureName = "TableSizeHistory_Get",
                                    ColumnToParameterMap = new Dictionary<string, string> { { "@ObjectID", "ObjectID" } }
                                }
                            }},
                            { "Rows", new ColumnMetadata {
                                FormatString = "N0"
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
                            { "Avg_Rows_Per_Day", new ColumnMetadata {
                                Alias = "Avg Rows Per Day",
                                FormatString = "N0"
                            }},
                            { "Avg_KB_Per_Day", new ColumnMetadata {
                                Alias = "Avg KB Per Day",
                                FormatString = "N0"
                            }},
                            { "CalcDays", new ColumnMetadata {
                                Alias = "Calc Days",
                                FormatString = "N1",
                                Visible = false
                            }},
                            { "SnapshotDate", new ColumnMetadata {
                                Alias = "Snapshot Date",
                                Highlighting = new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true }
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
                        { "60", "30 Days" },
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