using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    public class ServerServicesReport
    {
        public static SystemReport Instance => new()
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
    }
}