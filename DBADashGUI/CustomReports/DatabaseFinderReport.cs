using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    internal class DatabaseFinderReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            QualifiedProcedureName = "dbo.DatabaseFinder_Get",
            ProcedureName = "DatabaseFinder_Get",
            ReportName = "Database Finder",
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
                        ParamName = "@SearchString",
                        ParamType = "NVARCHAR",
                    },
                    new()
                    {
                        ParamName = "@Top",
                        ParamType = "INT",
                    },
                    new()
                    {
                        ParamName = "@ExcludeSystemDatabases",
                        ParamType = "BIT",
                    },
                },
            },
            Pickers = new List<Picker>
            {
                Picker.CreateTopPicker(true),
                Picker.CreateBooleanPicker("@ExcludeSystemDatabases","System Databases",true,"Exclude","Include",true),
                new()
                {
                    DataType = typeof(string),
                    ParameterName = "@SearchString",
                    Name = "Search (LIKE)",
                    IsText = true,
                    MenuBar = true
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        CellFormatString = new Dictionary<string, string>(),
                        CellNullValue = new Dictionary<string, string>(),
                        DoNotConvertToLocalTimeZone = new List<string>(),
                        ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
                        {
                            new("InstanceID", new PersistedColumnLayout {  Visible = false }),
                            new("InstanceDisplayName", new PersistedColumnLayout {  Visible = true }),
                            new("Database", new PersistedColumnLayout {  Visible = true }),
                            new("Performance", new PersistedColumnLayout {  Visible = true }),
                            new("Object Execution", new PersistedColumnLayout {  Visible = true }),
                            new("Slow Queries", new PersistedColumnLayout {  Visible = true }),
                            new("Files", new PersistedColumnLayout {  Visible = true }),
                            new("Snapshot Summary", new PersistedColumnLayout {  Visible = true }),
                            new("DB Space", new PersistedColumnLayout {  Visible = true }),
                            new("DB Configuration", new PersistedColumnLayout {  Visible = true }),
                            new("DB Options", new PersistedColumnLayout {  Visible = true }),
                            new("QS", new PersistedColumnLayout {  Visible = true }),
                            new("Top Queries (Query Store)", new PersistedColumnLayout {  Visible = true }),
                            new("Forced Plans (Query Store)", new PersistedColumnLayout {  Visible = true }),
                        },
                        ColumnAlias = new Dictionary<string, string>
                        {
                            { "InstanceDisplayName", "Instance" },
                        },
                        ResultName = "Result1",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            { "InstanceDisplayName", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", Tab = Main.Tabs.Performance} },
                            { "Database", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database" } },
                            { "Performance", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database" } },
                            { "Object Execution", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.ObjectExecutionSummary } },
                            { "Slow Queries", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.SlowQueries } },
                            { "Files", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.Files } },
                            { "Snapshot Summary", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.SnapshotSummary } },
                            { "DB Space", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.DBSpace } },
                            { "DB Configuration", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.DBConfiguration } },
                            { "DB Options", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.DBOptions } },
                            { "QS", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.QS } },
                            { "Top Queries (Query Store)", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.TopQueries } },
                            { "Forced Plans (Query Store)", new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.QueryStoreForcedPlans } }
                        },
                    }
                }
            }
        };
    }
}