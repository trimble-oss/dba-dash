using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    internal class SQLLoginPasswordAgeReport
    {
        public static SystemReport Instance => new()
        {
            ReportName = "SQL Login Password Age",
            Description = "SQL logins with the date the password was last changed (LOGINPROPERTY PasswordLastSetTime)",
            SchemaName = "dbo",
            ProcedureName = "SQLLoginPasswordAge_Get",
            QualifiedProcedureName = "dbo.SQLLoginPasswordAge_Get",
            ReportVisibilityRole = "SecurityReports",
            TriggerCollectionTypes = new List<string> { "ServerPrincipals" },
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
                        ParamName = "@Days",
                        ParamType = "INT"
                    },
                    new()
                    {
                        ParamName = "@IncludeDisabled",
                        ParamType = "BIT"
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
                    new()
                    {
                        ParamName = "@Top",
                        ParamType = "INT"
                    },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@Days",
                    Name = "Password Age (Days)",
                    PickerItems = new Dictionary<object, string>
                    {
                        { 0, "ALL" },
                        { 30, ">30" },
                        { 60, ">60" },
                        { 90, ">90" },
                        { 180, ">180" },
                        { 365, ">365" },
                    },
                    DefaultValue = 0,
                    MenuBar = true,
                    DataType = typeof(int)
                },
                Picker.CreateBooleanPicker("@IncludeDisabled", "Disabled Logins", false, "Include", "Exclude", true),
                Picker.CreateTopPicker(true)
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Summary",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "InstanceID", new ColumnMetadata { Visible = false } },
                            { "Instance", new ColumnMetadata {
                                Link = new DrillDownLinkColumnInfo
                                {
                                    ReportProcedureName = "SQLLoginPasswordAge_Get",
                                    CarryUserParameters = true,
                                    ColumnToParameterMap = new Dictionary<string, string>
                                    { { "@InstanceDisplayName", "Instance" } }
                                }
                            } },
                            { "SQL Logins", new ColumnMetadata { FormatString = "N0" } },
                            { "Max Password Age (Days)", new ColumnMetadata {
                                FormatString = "N0",
                                Description = "Number of days since the password was last set for the login with the oldest password"
                            } },
                            { "Unknown", new ColumnMetadata {
                                Description = "Password last set time not available.  Not yet collected by an agent that supports it, not visible to the collection account, or not recorded by the instance."
                            } }
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "Oldest Passwords",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "InstanceID", new ColumnMetadata { Visible = false } },
                            { "Instance", new ColumnMetadata {
                                Link = new DrillDownLinkColumnInfo
                                {
                                    ReportProcedureName = "SQLLoginPasswordAge_Get",
                                    CarryUserParameters = true,
                                    ColumnToParameterMap = new Dictionary<string, string>
                                    { { "@InstanceDisplayName", "Instance" } }
                                }
                            } },
                            { "Login", new ColumnMetadata {
                                Link = new DrillDownLinkColumnInfo
                                {
                                    ReportProcedureName = "SQLLoginPasswordAge_Get",
                                    CarryUserParameters = true,
                                    ColumnToParameterMap = new Dictionary<string, string>
                                    { { "@Login", "Login" } }
                                }
                            } },
                            { "Is Disabled", new ColumnMetadata() },
                            { "Created Date", new ColumnMetadata() },
                            { "Modified Date", new ColumnMetadata() },
                            { "Password Last Set", new ColumnMetadata {
                                Description = "PasswordLastSetTime from LOGINPROPERTY.  Blank if unknown."
                            } },
                            { "Password Age (Days)", new ColumnMetadata {
                                FormatString = "N0",
                                Description = "Number of days since the password was last set"
                            } }
                        }
                    }
                }
            }
        };
    }
}
