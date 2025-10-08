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
                        ResultName = "New Databases",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "InstanceID", new ColumnMetadata() },
                            { "InstanceGroupName", new ColumnMetadata {
                                Alias = "Instance",
                                DisplayIndex = 1,
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "" },
                                Description="Instance name.  Click cell to navigate to instance & load Performance tab"
                            }},
                            { "DB", new ColumnMetadata {
                                DisplayIndex = 2,
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "DB" },
                                Description="Database name.  Click cell to navigate to database & load Performance tab"
                            }},
                            { "CreatedDate", new ColumnMetadata {
                                Alias = "Created Date",
                                DisplayIndex = 3,
                                Description="create_date from sys.databases"
                            }},
                            { "SizeMB", new ColumnMetadata {
                                Alias = "Size (MB)",
                                FormatString = "N0",
                                DisplayIndex = 4,
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "DB", Tab = Main.Tabs.DBSpace },
                                Description = "Size of the database in megabytes. Click cell to navigate to database & load DB Space tab"
                            }}
                        }
                    }
                }
            }
        };
    }
}