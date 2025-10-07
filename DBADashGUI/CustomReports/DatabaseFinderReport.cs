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
                            { "InstanceDisplayName", new ColumnMetadata {
                                Alias = "Instance",
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", Tab = Main.Tabs.Performance }
                            }},
                            { "Database", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database" }
                            }},
                            { "Performance", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database" }
                            }},
                            { "Object Execution", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.ObjectExecutionSummary }
                            }},
                            { "Slow Queries", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.SlowQueries }
                            }},
                            { "Files", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.Files }
                            }},
                            { "Snapshot Summary", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.SnapshotSummary }
                            }},
                            { "DB Space", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.DBSpace }
                            }},
                            { "DB Configuration", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.DBConfiguration }
                            }},
                            { "DB Options", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.DBOptions }
                            }},
                            { "QS", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.QS }
                            }},
                            { "Top Queries (Query Store)", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.TopQueries }
                            }},
                            { "Forced Plans (Query Store)", new ColumnMetadata {
                                Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "Database", Tab = Main.Tabs.QueryStoreForcedPlans }
                            }}
                        }
                    }
                }
            }
        };
    }
}