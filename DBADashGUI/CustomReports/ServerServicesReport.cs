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
                                Visible = false,
                                Description = "DBA Dash instance identifier from dbo.Instances"
                            }},
                            { "InstanceDisplayName", new ColumnMetadata {
                                Alias = "Instance",
                                Link = new DrillDownLinkColumnInfo {
                                    ReportProcedureName = "ServerServices_Get",
                                    ColumnToParameterMap = new Dictionary<string, string> { { "@InstanceIDs", "InstanceID" } }
                                },
                                Description = "Name of instance"
                            }},
                            { "service_type", new ColumnMetadata {
                                Alias = "Service Type",
                                Link = new DrillDownLinkColumnInfo {
                                    ReportProcedureName = "ServerServices_Get",
                                    ColumnToParameterMap = new Dictionary<string, string> { { "@ServiceType", "service_type" } }
                                },
                                Description = "Type of service. e.g. SQL Server, SQL Server Agent"
                            }},
                            { "servicename", new ColumnMetadata {
                                Alias = "Service Name",
                                Description = "Name of the service"
                            }},
                            { "startup_type", new ColumnMetadata {
                                Alias = "Startup Type (ID)",
                                Visible = false,
                                Description = "Service startup type. \n0: Other\r\n1: Other\r\n2: Automatic\r\n3: Manual\r\n4: Disabled"
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
                                },
                                Description = "Service startup type. \n0: Other\r\n1: Other\r\n2: Automatic\r\n3: Manual\r\n4: Disabled"
                            }},
                            { "status", new ColumnMetadata {
                                Alias = "Status (ID)",
                                Visible = false,
                                Description = "Service status. \n1: Stopped\r\n2: Other (start pending)\r\n3: Other (stop pending)\r\n4: Running\r\n5: Other (continue pending)\r\n6: Other (pause pending)\r\n7: Paused"
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
                                },
                                Description = "Service status. \n1: Stopped\r\n2: Other (start pending)\r\n3: Other (stop pending)\r\n4: Running\r\n5: Other (continue pending)\r\n6: Other (pause pending)\r\n7: Paused"
                            }},
                            { "process_id", new ColumnMetadata {
                                Alias = "Process ID",
                                Description = "Process ID of the service if running"
                            }},
                            { "last_startup_time", new ColumnMetadata {
                                Alias = "Last Startup Time",
                                Description = "Date and time of the last service startup"
                            }},
                            { "service_account", new ColumnMetadata {
                                Alias = "Service Account",
                                Link = new DrillDownLinkColumnInfo {
                                    ReportProcedureName = "ServerServices_Get",
                                    ColumnToParameterMap = new Dictionary<string, string> { { "@ServiceAccount", "service_account" } }
                                },
                                Description = "Account that the service runs under"
                            }},
                            { "is_managed_service_account", new ColumnMetadata {
                                Alias = "Managed Service Account?",
                                Description = "Indicates if the service account is a managed service account"
                            }},
                            { "filename", new ColumnMetadata {
                                Alias = "FileName",
                                Description = "Path to the service executable"
                            }},
                            { "is_clustered", new ColumnMetadata {
                                Alias = "Is Clustered",
                                Description = "Indicates whether the service is installed as a resource of a clustered server."
                            }},
                            { "cluster_nodename", new ColumnMetadata {
                                Alias = "Cluster Node Name",
                                Description = "The name of the cluster node where the service is installed"
                            }},
                            { "instant_file_initialization_enabled", new ColumnMetadata {
                                Alias = "Instant File Initialization Enabled",
                                Highlighting = new CellHighlightingRuleSet("instant_file_initialization_enabled_status") { IsStatusColumn = true },
                                Description = "Indicates if instant file initialization is enabled (Recommended). Applies to SQL Server service type only."
                            }},
                            { "instant_file_initialization_enabled_status", new ColumnMetadata {
                                Alias = "Instant File Initialization Status",
                                Visible = false,
                                Highlighting = new CellHighlightingRuleSet("instant_file_initialization_enabled_status") { IsStatusColumn = true },
                                Description = "DBA Dash status value for instant file initialization"
                            }},
                            { "SnapshotDate", new ColumnMetadata {
                                Alias = "Snapshot Date",
                                Highlighting = new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true },
                                Description = "Date and time of the collection"
                            }},
                            { "SnapshotStatus", new ColumnMetadata {
                                Alias = "Snapshot Status",
                                Visible = false,
                                Highlighting = new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true },
                                Description="Status value to indicate if the collection is stale, based on collection date threshold for the collection type"
                            }}
                        }
                    }
                }
            },
        };
    }
}