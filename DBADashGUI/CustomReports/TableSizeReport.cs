using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    internal class TableSizeReport
    {
        public static SystemReport Instance => new()
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
                                new("ObjectID", new PersistedColumnLayout {  Visible = false }),
                                new("InstanceID", new PersistedColumnLayout {  Visible = false }),
                                new("Instance", new PersistedColumnLayout {  Visible = true }),
                                new("DB", new PersistedColumnLayout {  Visible = true }),
                                new("SchemaName", new PersistedColumnLayout {  Visible = true }),
                                new("ObjectName", new PersistedColumnLayout {  Visible = true }),
                                new("Rows", new PersistedColumnLayout {  Visible = true }),
                                new("Reserved_KB", new PersistedColumnLayout {  Visible = true }),
                                new("Used_KB", new PersistedColumnLayout {  Visible = true }),
                                new("Data_KB", new PersistedColumnLayout { Visible = true }),
                                new("Index_KB", new PersistedColumnLayout {  Visible = true }),
                                new("Avg_Rows_Per_Day", new PersistedColumnLayout {  Visible = true }),
                                new("Avg_KB_Per_Day", new PersistedColumnLayout {  Visible = true }),
                                new("CalcDays", new PersistedColumnLayout { Visible = false }),
                                new("SnapshotDate", new PersistedColumnLayout { Visible = true }),
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
                                        ColumnToParameterMap = new Dictionary<string, string>
                                            { { "@ObjectID", "ObjectID" } }
                                    }
                                }
                            }
                        }
                    }
        },
            TriggerCollectionTypes = { DBADash.CollectionType.TableSize.ToString() },
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