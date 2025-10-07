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
                        ResultName = "Server Services",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "InstanceID", new ColumnMetadata {
                                Alias = "Instance ID",
                                Visible = false
                            }},
                            { "InstanceDisplayName", new ColumnMetadata {
                                Alias = "Instance",
                                Link = new DrillDownLinkColumnInfo {
                                    ReportProcedureName = "ServerServices_Get",
                                    ColumnToParameterMap = new Dictionary<string, string> { { "@InstanceIDs", "InstanceID" } }
                                }
                            }},
                            { "service_type", new ColumnMetadata {
                                Alias = "Service Type",
                                Link = new DrillDownLinkColumnInfo {
                                    ReportProcedureName = "ServerServices_Get",
                                    ColumnToParameterMap = new Dictionary<string, string> { { "@ServiceType", "service_type" } }
                                }
                            }},
                            { "servicename", new ColumnMetadata {
                                Alias = "Service Name"
                            }},
                            { "startup_type", new ColumnMetadata {
                                Alias = "Startup Type (ID)",
                                Visible = false
                            }},
                            { "startup_type_desc", new ColumnMetadata {
                                Alias = "Startup Type",
                                Highlighting = new CellHighlightingRuleSet("startup_type_desc")
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
                            }},
                            { "status", new ColumnMetadata {
                                Alias = "Status (ID)",
                                Visible = false
                            }},
                            { "status_desc", new ColumnMetadata {
                                Alias = "Status",
                                Highlighting = new CellHighlightingRuleSet("status_desc")
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
                            }},
                            { "process_id", new ColumnMetadata {
                                Alias = "Process ID"
                            }},
                            { "last_startup_time", new ColumnMetadata {
                                Alias = "Last Startup Time"
                            }},
                            { "service_account", new ColumnMetadata {
                                Alias = "Service Account",
                                Link = new DrillDownLinkColumnInfo {
                                    ReportProcedureName = "ServerServices_Get",
                                    ColumnToParameterMap = new Dictionary<string, string> { { "@ServiceAccount", "service_account" } }
                                }
                            }},
                            { "is_managed_service_account", new ColumnMetadata {
                                Alias = "Managed Service Account?"
                            }},
                            { "filename", new ColumnMetadata {
                                Alias = "FileName"
                            }},
                            { "is_clustered", new ColumnMetadata {
                                Alias = "Is Clustered"
                            }},
                            { "cluster_nodename", new ColumnMetadata {
                                Alias = "Cluster Node Name"
                            }},
                            { "instant_file_initialization_enabled", new ColumnMetadata {
                                Alias = "Instant File Initialization Enabled",
                                Highlighting = new CellHighlightingRuleSet("instant_file_initialization_enabled_status") { IsStatusColumn = true }
                            }},
                            { "instant_file_initialization_enabled_status", new ColumnMetadata {
                                Alias = "Instant File Initialization Status",
                                Visible = false,
                                Highlighting = new CellHighlightingRuleSet("instant_file_initialization_enabled_status") { IsStatusColumn = true }
                            }},
                            { "SnapshotDate", new ColumnMetadata {
                                Alias = "Snapshot Date",
                                Highlighting = new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true }
                            }},
                            { "SnapshotStatus", new ColumnMetadata {
                                Alias = "Snapshot Status",
                                Visible = false,
                                Highlighting = new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true }
                            }}
                        }
                    }
                }
            },
        };
    }
}