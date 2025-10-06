using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    internal class NewDatabasesReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ReportName = "New Databases",
            Description = "Databases with a recent create_date (sys.databases)",
            TriggerCollectionTypes = new List<string> { "Databases" },
            ProcedureName = "NewDatabases_Get",
            QualifiedProcedureName = "dbo.NewDatabases_Get",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new()
                    {
                        ParamName = "@Days",
                        ParamType = "INT"
                    },
                    new()
                    {
                        ParamName = "@InstanceIDs",
                        ParamType = "IDS"
                    },
                },
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@Days",
                    Name = "Days",
                    PickerItems = new Dictionary<object, string>
                    {
                        { 1, "1" },
                        { 2, "2" },
                        { 3, "3" },
                        { 4, "4" },
                        { 5, "5" },
                        { 6, "6" },
                        { 7, "7" },
                        { 14, "14" },
                        { 30, "30" },
                        { 60, "60" },
                        { 90, "90" },
                        { 180, "180" },
                        { 365, "365" },
                    },
                    DefaultValue = 7,
                    MenuBar = true,
                    DataType = typeof(int)
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ColumnAlias = new Dictionary<string, string>
                        {
                            { "InstanceGroupName", "Instance" },
                            { "CreatedDate", "Created Date" },
                            { "SizeMB", "Size (MB)" }
                        },
                        CellFormatString = new Dictionary<string, string>
                        {
                            { "SizeMB", "N0" }
                        },
                        ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>
                        {
                            new("InstanceID", new PersistedColumnLayout()),
                            new("InstanceGroupName", new PersistedColumnLayout { Visible = true, DisplayIndex = 1 }),
                            new("DB", new PersistedColumnLayout { Visible = true, DisplayIndex = 2 }),
                            new("CreatedDate", new PersistedColumnLayout {  Visible = true, DisplayIndex = 3 }),
                            new("SizeMB", new PersistedColumnLayout { Visible = true, DisplayIndex = 4 })
                        },
                        ResultName = "New Databases",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            { "DB", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "DB" } },
                            { "SizeMB", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "DB", Tab = Main.Tabs.DBSpace } },
                            { "InstanceGroupName", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "" } }
                        },
                    }
                }
            }
        };
    }
}