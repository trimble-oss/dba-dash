using System.Collections.Generic;
using Amazon.S3.Model;

namespace DBADashGUI.CustomReports
{
    public class SystemReports : List<SystemReport>
    {
        public SystemReports()
        {
            Add(TableSizeReport);
            Add(TableSizeHistory);
        }

        public static SystemReport TableSizeReport =>
            new()
            {
                ReportName = "Table Size",
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
                                { "Avg_Rows_Per_Day", "Avg Rows Per Day" },
                                { "Avg_KB_Per_Day", "Avg KB Per Day" },
                                { "CalcDays", "Calc Days" }
                            },
                            CellFormatString = new Dictionary<string, string>
                            {
                                { "Rows", "N0" },
                                { "Reserved_KB", "N0" },
                                { "Used_KB", "N0" },
                                { "Data_KB", "N0" },
                                { "Index_KB", "N0" },
                                { "Avg_Rows_Per_Day", "N0" },
                                { "Avg_KB_Per_Day", "N0" },
                                { "CalcDays", "N1" }
                            },
                            ResultName = "Table Size",
                            ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
                            {
                                new("ObjectID", new PersistedColumnLayout { Width = 100, Visible = false }),
                                new("InstanceID", new PersistedColumnLayout { Width = 100, Visible = false }),
                                new("Instance", new PersistedColumnLayout { Width = 150, Visible = true }),
                                new("DB", new PersistedColumnLayout { Width = 150, Visible = true }),
                                new("SchemaName", new PersistedColumnLayout { Width = 100, Visible = true }),
                                new("ObjectName", new PersistedColumnLayout { Width = 150, Visible = true }),
                                new("Rows", new PersistedColumnLayout { Width = 70, Visible = true }),
                                new("Reserved_KB", new PersistedColumnLayout { Width = 100, Visible = true }),
                                new("Used_KB", new PersistedColumnLayout { Width = 100, Visible = true }),
                                new("Data_KB", new PersistedColumnLayout { Width = 100, Visible = true }),
                                new("Index_KB", new PersistedColumnLayout { Width = 100, Visible = true }),
                                new("Avg_Rows_Per_Day", new PersistedColumnLayout { Width = 100, Visible = true }),
                                new("Avg_KB_Per_Day", new PersistedColumnLayout { Width = 100, Visible = true }),
                                new("CalcDays", new PersistedColumnLayout { Width = 100, Visible = false }),
                                new("SnapshotDate", new PersistedColumnLayout { Width = 120, Visible = true }),
                            },
                            CellHighlightingRules =
                            {
                                {
                                    "SnapshotDate",
                                    new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true }
                                }
                            },
                            LinkColumns = new Dictionary<string, LinkColumnInfo>
                            {
                                {
                                    "ObjectName",
                                    new DrillDownLinkColumnInfo
                                    {
                                        ReportProcedureName = "TableSizeHistory_Get",
                                        ColumnToParameterMap = new Dictionary<string, string> { { "@ObjectID", "ObjectID" } }
                                    }
                                }
                            }
                        }
                    }
                },
                SchemaName = "dbo",
                ProcedureName = "TableSize_Get",
                QualifiedProcedureName = "dbo.TableSize_Get",
                CanEditReport = false,
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
                            {"1", "1 Day"},
                            {"2", "2 Days"},
                            {"7", "7 Days"},
                            {"14", "14 Days"},
                            {"30", "30 Days"},
                            {"60", "30 Days"},
                            {"90", "90 Days"},
                            {"365", "365 Days"}
                        }
                    },
                }
            };

        public static SystemReport TableSizeHistory =>
            new()
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