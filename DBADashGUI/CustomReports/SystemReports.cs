using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    public class SystemReports : List<SystemReport>
    {
        public SystemReports()
        {
            Add(DatabaseFinder);
            Add(DeletedDatabases);
            Add(NewDatabases);
            Add(ServerRoleMembers);
            Add(ServerServices);
            Add(TableSizeHistory);
            Add(TableSizeReport);
        }

        public static SystemReport ServerServices => new()
        {
            ReportName = "Server Services",
            SchemaName = "dbo",
            ProcedureName = "ServerServices_Get",
            QualifiedProcedureName = "dbo.ServerServices_Get",
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
                        ParamName = "@ServiceType",
                        ParamType = "NVARCHAR"
                    },
                    new()
                    {
                        ParamName = "@ServiceAccount",
                        ParamType = "NVARCHAR",
                    },
                },
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@ServiceType",
                    Name = "Service Type",
                    DefaultValue = null,
                    PickerItems = new()
                    {
                        { "SQL Full-text Filter Daemon Launcher", "SQL Full-text Filter Daemon Launcher" },
                        { "SQL Server", "SQL Server" },
                        { "SQL Server Agent", "SQL Server Agent" },
                        { "", "ALL" }
                    },
                    MenuBar = true
                },
            },
            TriggerCollectionTypes = { DBADash.CollectionType.ServerServices.ToString() },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ColumnAlias = new Dictionary<string, string>
                        {
                            { "InstanceID", "Instance ID" },
                            { "InstanceDisplayName", "Instance" },
                            { "service_type", "Service Type" },
                            { "servicename", "Service Name" },
                            { "startup_type", "Startup Type (ID)" },
                            { "startup_type_desc", "Startup Type" },
                            { "status", "Status (ID)" },
                            { "status_desc", "Status" },
                            { "process_id", "Process ID" },
                            { "last_startup_time", "Last Startup Time" },
                            { "service_account", "Service Account" },
                            { "is_managed_service_account", "Managed Service Account?" },
                            { "filename", "FileName" },
                            { "is_clustered", "Is Clustered" },
                            { "cluster_nodename", "Cluster Node Name" },
                            { "instant_file_initialization_enabled", "Instant File Initialization Enabled" },
                            { "instant_file_initialization_enabled_status", "Instant File Initialization Status" },
                            { "SnapshotDate", "Snapshot Date" },
                            { "SnapshotStatus", "Snapshot Status" }
                        },
                        ResultName = "Server Services",
                        ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
                        {
                            new("InstanceID", new PersistedColumnLayout { Visible = false }),
                            new("InstanceDisplayName", new PersistedColumnLayout {  Visible = true }),
                            new("service_type", new PersistedColumnLayout {  Visible = true }),
                            new("servicename", new PersistedColumnLayout {  Visible = true }),
                            new("startup_type", new PersistedColumnLayout {  Visible = false }),
                            new("startup_type_desc", new PersistedColumnLayout {  Visible = true }),
                            new("status", new PersistedColumnLayout {  Visible = false }),
                            new("status_desc", new PersistedColumnLayout {  Visible = true }),
                            new("process_id", new PersistedColumnLayout {  Visible = true }),
                            new("last_startup_time", new PersistedColumnLayout {  Visible = true }),
                            new("service_account", new PersistedColumnLayout {  Visible = true }),
                            new("is_managed_service_account", new PersistedColumnLayout {  Visible = true }),
                            new("filename", new PersistedColumnLayout {  Visible = true }),
                            new("is_clustered", new PersistedColumnLayout {  Visible = true }),
                            new("cluster_nodename", new PersistedColumnLayout {  Visible = true }),
                            new("instant_file_initialization_enabled",
                                new PersistedColumnLayout {  Visible = true }),
                            new("instant_file_initialization_enabled_status",
                                new PersistedColumnLayout {  Visible = false }),
                            new("SnapshotDate", new PersistedColumnLayout {  Visible = true }),
                            new("SnapshotStatus", new PersistedColumnLayout {  Visible = false }),
                        },
                        CellHighlightingRules =
                        {
                            {
                                "SnapshotDate",
                                new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true }
                            },
                            {
                                "SnapshotStatus",
                                new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true }
                            },
                            {
                                "instant_file_initialization_enabled",
                                new CellHighlightingRuleSet("instant_file_initialization_enabled_status")
                                    { IsStatusColumn = true }
                            },
                            {
                                "instant_file_initialization_enabled_status",
                                new CellHighlightingRuleSet("instant_file_initialization_enabled_status")
                                    { IsStatusColumn = true }
                            },
                            {
                                "startup_type_desc",
                                new CellHighlightingRuleSet("startup_type_desc")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = "Automatic",
                                            Status = DBADashStatus.DBADashStatusEnum.OK
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = "Manual",
                                            Status = DBADashStatus.DBADashStatusEnum.NA
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.All,
                                            Status = DBADashStatus.DBADashStatusEnum.WarningLow
                                        }
                                    }
                                }
                            },
                            {
                                "status_desc",
                                new CellHighlightingRuleSet("status_desc")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = "Running",
                                            Status = DBADashStatus.DBADashStatusEnum.OK
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.All,
                                            Status = DBADashStatus.DBADashStatusEnum.WarningLow
                                        }
                                    }
                                }
                            }
                        },
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "service_type",
                                new DrillDownLinkColumnInfo
                                {
                                    ReportProcedureName = "ServerServices_Get",
                                    ColumnToParameterMap = new Dictionary<string, string>
                                        { { "@ServiceType", "service_type" } }
                                }
                            },
                            {
                                "service_account",
                                new DrillDownLinkColumnInfo
                                {
                                    ReportProcedureName = "ServerServices_Get",
                                    ColumnToParameterMap = new Dictionary<string, string>
                                        { { "@ServiceAccount", "service_account" } }
                                }
                            },
                            {
                                "InstanceDisplayName",
                                new DrillDownLinkColumnInfo
                                {
                                    ReportProcedureName = "ServerServices_Get",
                                    ColumnToParameterMap = new Dictionary<string, string>
                                        { { "@InstanceIDs", "InstanceID" } }
                                }
                            }
                        }
                    }
                }
            },
        };

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

        public static SystemReport ServerRoleMembers => new()
        {
            ReportName = "Server Role Members (sysadmin)",
            Description = "Server Role Members - showing sysadmin users by default",
            SchemaName = "dbo",
            ProcedureName = "ServerRoleMembers_Get",
            QualifiedProcedureName = "dbo.ServerRoleMembers_Get",
            ReportVisibilityRole = "SecurityReports",
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
                        ParamName = "@IsDisabled",
                        ParamType = "BIT"
                    },
                    new()
                    {
                        ParamName = "@TypeDesc",
                        ParamType = "NVARCHAR",
                    },
                    new()
                    {
                        ParamName = "@Type",
                        ParamType = "CHAR",
                    },
                    new()
                    {
                        ParamName = "@ServerRole",
                        ParamType = "NVARCHAR",
                    },
                    new()
                    {
                        ParamName = "@Login",
                        ParamType = "NVARCHAR",
                    },
                    new()
                    {
                        ParamName = "@InstanceDisplayName",
                        ParamType = "NVARCHAR",
                    },
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
                {
                    {
                        0, new CustomReportResult
                        {
                            ResultName = "Normal",
                            ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
                            {
                                new("Instance", new PersistedColumnLayout {  Visible = true }),
                                new("Login", new PersistedColumnLayout {  Visible = true }),
                                new("Type", new PersistedColumnLayout {  Visible = true }),
                                new("Type Description", new PersistedColumnLayout { Visible = true }),
                                new("Is Disabled", new PersistedColumnLayout {  Visible = true }),
                                new("Created Date", new PersistedColumnLayout {  Visible = true }),
                                new("Modified Date", new PersistedColumnLayout {  Visible = true })
                            },
                            LinkColumns = new Dictionary<string, LinkColumnInfo>
                            {
                                {
                                    "Type Description",
                                    new DrillDownLinkColumnInfo
                                    {
                                        ReportProcedureName = "ServerRoleMembers_Get",
                                        ColumnToParameterMap = new Dictionary<string, string>
                                            { { "@Type", "Type" } }
                                    }
                                },
                                {
                                    "Type",
                                    new DrillDownLinkColumnInfo
                                    {
                                        ReportProcedureName = "ServerRoleMembers_Get",
                                        ColumnToParameterMap = new Dictionary<string, string>
                                            { { "@Type", "Type" } }
                                    }
                                },
                                {
                                    "Login",
                                    new DrillDownLinkColumnInfo
                                    {
                                        ReportProcedureName = "ServerRoleMembers_Get",
                                        ColumnToParameterMap = new Dictionary<string, string>
                                            { { "@Login", "Login" } }
                                    }
                                },
                                {
                                    "Instance",
                                    new DrillDownLinkColumnInfo
                                    {
                                        ReportProcedureName = "ServerRoleMembers_Get",
                                        ColumnToParameterMap = new Dictionary<string, string>
                                            { { "@InstanceDisplayName", "Instance" } }
                                    }
                                }
                            }
                        }
                    },
                    {
                        1, new CustomReportResult
                        {
                            ResultName = "Pivot By Instance"
                        }
                    },
                    {
                        2, new CustomReportResult
                        {
                            ResultName = "Pivot By User"
                        }
                    }
                },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@IsDisabled",
                    Name = "Is Disabled",
                    DefaultValue = "",
                    PickerItems = new()
                    {
                        { "true", "Y" },
                        { "false", "N" },
                        { "", "ALL" }
                    }
                },
                new()
                {
                    ParameterName = "@Type",
                    Name = "Type",
                    DefaultValue = "",
                    PickerItems = new()
                    {
                        { "C","CERTIFICATE_MAPPED_LOGIN" },
                        { "G","WINDOWS_GROUP" },
                        { "S","SQL_LOGIN" },
                        { "U","WINDOWS_LOGIN" },
                        { "", "ALL" }
                    }
                },
                new()
                {
                    ParameterName = "@ServerRole",
                    Name = "Server Role",
                    DefaultValue = "sysadmin",
                    PickerItems = new()
                    {
                        { "##MS_DatabaseConnector##", "##MS_DatabaseConnector##" },
                        { "##MS_DatabaseManager##", "##MS_DatabaseManager##" },
                        { "##MS_DefinitionReader##", "##MS_DefinitionReader##" },
                        { "##MS_LoginManager##", "##MS_LoginManager##" },
                        { "##MS_PerformanceDefinitionReader##", "##MS_PerformanceDefinitionReader##" },
                        { "##MS_SecurityDefinitionReader##", "##MS_SecurityDefinitionReader##" },
                        { "##MS_ServerPerformanceStateReader##", "##MS_ServerPerformanceStateReader##" },
                        { "##MS_ServerSecurityStateReader##", "##MS_ServerSecurityStateReader##" },
                        { "##MS_ServerStateManager##", "##MS_ServerStateManager##" },
                        { "##MS_ServerStateReader##", "##MS_ServerStateReader##" },
                        { "bulkadmin", "bulkadmin" },
                        { "dbcreator", "dbcreator" },
                        { "diskadmin", "diskadmin" },
                        { "processadmin", "processadmin" },
                        { "securityadmin", "securityadmin" },
                        { "serveradmin", "serveradmin" },
                        { "setupadmin", "setupadmin" },
                        { "sysadmin", "sysadmin" }
                    }
                },
            }
        };

        public static SystemReport DatabaseFinder => new()
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
        public static SystemReport DeletedDatabases => new()
        {
            SchemaName = "dbo",
            ReportName = "Deleted Databases",
            Description = "Databases deleted in the last N days (default 7 days)",
            TriggerCollectionTypes = new List<string>(),
            ProcedureName = "DeletedDatabases_Get",
            QualifiedProcedureName = "dbo.DeletedDatabases_Get",
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
                }
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
                            { "DB", "DB" },
                            { "CreatedDate", "Created Date" },
                            { "DeletedDate", "~Deleted Date" },
                            {"SizeMB", "Size MB" }
                        },
                        CellFormatString = new Dictionary<string, string>
                        {
                            { "SizeMB", "N0" }
                        },
                        ResultName = "Deleted Databases",
                    }
                }
            }
        };
        public static SystemReport NewDatabases => new()
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