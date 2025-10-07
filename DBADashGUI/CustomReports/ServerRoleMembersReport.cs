using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    internal class ServerRoleMembersReport
    {
        public static SystemReport Instance => new()
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
                            Columns = new Dictionary<string, ColumnMetadata>
                            {
                                { "Instance", new ColumnMetadata {
                                    Link = new DrillDownLinkColumnInfo
                                    {
                                        ReportProcedureName = "ServerRoleMembers_Get",
                                        ColumnToParameterMap = new Dictionary<string, string>
                                        { { "@InstanceDisplayName", "Instance" } }
                                    }
                                } },
                                { "Login", new ColumnMetadata {
                                    Link = new DrillDownLinkColumnInfo
                                    {
                                        ReportProcedureName = "ServerRoleMembers_Get",
                                        ColumnToParameterMap = new Dictionary<string, string>
                                        { { "@Login", "Login" } }
                                    }
                                } },
                                { "Type", new ColumnMetadata {
                                    Link = new DrillDownLinkColumnInfo
                                    {
                                        ReportProcedureName = "ServerRoleMembers_Get",
                                        ColumnToParameterMap = new Dictionary<string, string>
                                        { { "@Type", "Type" } }
                                    }
                                } },
                                { "Type Description", new ColumnMetadata {
                                    Link = new DrillDownLinkColumnInfo
                                    {
                                        ReportProcedureName = "ServerRoleMembers_Get",
                                        ColumnToParameterMap = new Dictionary<string, string>
                                        { { "@Type", "Type" } }
                                    }
                                } },
                                { "Is Disabled", new ColumnMetadata() },
                                { "Created Date", new ColumnMetadata() },
                                { "Modified Date", new ColumnMetadata() }
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