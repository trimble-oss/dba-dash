using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    public class SystemReports : List<SystemReport>
    {
        public SystemReports()
        {
            Add(TableSizeReport);
            Add(TableSizeHistory);
            Add(ServerRoleMembers);
            Add(ServerServices);
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
                    }
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
                            new("InstanceID", new PersistedColumnLayout { Width = 100, Visible = false }),
                            new("InstanceDisplayName", new PersistedColumnLayout { Width = 120, Visible = true }),
                            new("service_type", new PersistedColumnLayout { Width = 230, Visible = true }),
                            new("servicename", new PersistedColumnLayout { Width = 340, Visible = true }),
                            new("startup_type", new PersistedColumnLayout { Width = 70, Visible = false }),
                            new("startup_type_desc", new PersistedColumnLayout { Width = 110, Visible = true }),
                            new("status", new PersistedColumnLayout { Width = 75, Visible = false }),
                            new("status_desc", new PersistedColumnLayout { Width = 75, Visible = true }),
                            new("process_id", new PersistedColumnLayout { Width = 70, Visible = true }),
                            new("last_startup_time", new PersistedColumnLayout { Width = 110, Visible = true }),
                            new("service_account", new PersistedColumnLayout { Width = 240, Visible = true }),
                            new("is_managed_service_account", new PersistedColumnLayout { Width = 70, Visible = true }),
                            new("filename", new PersistedColumnLayout { Width = 150, Visible = true }),
                            new("is_clustered", new PersistedColumnLayout { Width = 70, Visible = true }),
                            new("cluster_nodename", new PersistedColumnLayout { Width = 110, Visible = true }),
                            new("instant_file_initialization_enabled",
                                new PersistedColumnLayout { Width = 80, Visible = true }),
                            new("instant_file_initialization_enabled_status",
                                new PersistedColumnLayout { Width = 80, Visible = false }),
                            new("SnapshotDate", new PersistedColumnLayout { Width = 110, Visible = true }),
                            new("SnapshotStatus", new PersistedColumnLayout { Width = 70, Visible = false }),
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
                                new("ObjectID", new PersistedColumnLayout { Width = 100, Visible = false }),
                                new("InstanceID", new PersistedColumnLayout { Width = 100, Visible = false }),
                                new("Instance", new PersistedColumnLayout { Width = 200, Visible = true }),
                                new("DB", new PersistedColumnLayout { Width = 150, Visible = true }),
                                new("SchemaName", new PersistedColumnLayout { Width = 100, Visible = true }),
                                new("ObjectName", new PersistedColumnLayout { Width = 200, Visible = true }),
                                new("Rows", new PersistedColumnLayout { Width = 100, Visible = true }),
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
                                new("Instance", new PersistedColumnLayout { Width = 250, Visible = true }),
                                new("Login", new PersistedColumnLayout { Width = 250, Visible = true }),
                                new("Type", new PersistedColumnLayout { Width = 60, Visible = true }),
                                new("Type Description", new PersistedColumnLayout { Width = 150, Visible = true }),
                                new("Is Disabled", new PersistedColumnLayout { Width = 70, Visible = true }),
                                new("Created Date", new PersistedColumnLayout { Width = 110, Visible = true }),
                                new("Modified Date", new PersistedColumnLayout { Width = 110, Visible = true })
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
    }
}