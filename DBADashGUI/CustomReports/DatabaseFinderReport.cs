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
                        ResultName = "Result1",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "InstanceID", new ColumnMetadata { Visible = false } },
                            { "DatabaseID", new ColumnMetadata { Visible = false } },
                            { "InstanceDisplayName", new ColumnMetadata {
                                Alias = "Instance",
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", Tab = Main.Tabs.Performance },
                                Description="Click cell to navigate to instance & load Performance tab"
                            }},
                            { "Database", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database" },
                                 Description="Click cell to navigate to database & load Performance tab"
                            }},
                            { "Performance", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database" },
                                Description="Click cell to navigate to database & load Performance tab"
                            }},
                            { "Object Execution", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.ObjectExecutionSummary },
                                Description="Click cell to navigate to database & load Object Execution tab"
                            }},
                            { "Slow Queries", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.SlowQueries },
                                Description="Click cell to navigate to database & load Slow Queries tab"
                            }},
                            { "Files", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.Files },
                                Description="Click cell to navigate to database & load Files tab"
                            }},
                            { "Snapshot Summary", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.SnapshotSummary },
                                Description="Click cell to navigate to database & load Snapshot Summary tab"
                            }},
                            { "DB Space", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.DBSpace },
                                Description="Click cell to navigate to database & load DB Space tab"
                            }},
                            { "DB Configuration", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.DBConfiguration },
                                Description="Click cell to navigate to database & load DB Configuration tab"
                            }},
                            { "DB Options", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.DBOptions },
                                Description="Click cell to navigate to database & load DB Options tab"
                            }},
                            { "QS", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.QS },
                                Description="Click cell to navigate to database & load Query Store tab"
                            }},
                            { "Top Queries (Query Store)", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.TopQueries },
                                Description="Click cell to navigate to database & load Top Queries tab"
                            }},
                            { "Forced Plans (Query Store)", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.QueryStoreForcedPlans },
                                Description="Click cell to navigate to database & load Forced Plans tab"
                            }}
                        }
                    }
                }
            }
        };
    }
}