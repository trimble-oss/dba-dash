using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    internal class DatabaseExtendedPropertiesReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            QualifiedProcedureName = "dbo.DatabaseExtendedProperties_Get",
            ProcedureName = "DatabaseExtendedProperties_Get",
            ReportName = "Database Extended Properties",
            Description = "Database Extended Properties",
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
                        ParamName = "@DatabaseID",
                        ParamType = "INT"
                    },
                    new()
                    {
                        ParamName = "@InstanceID",
                        ParamType = "INT"
                    }
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "InstanceID", new ColumnMetadata { Visible = false, Description = "DBA Dash instance identifier" } },
                            { "DatabaseID", new ColumnMetadata { Visible = false, Description = "DBA Dash database identifier" } },
                            { "InstanceDisplayName", new ColumnMetadata { Alias = "Instance", Description = "Instance Display Name" } },
                            { "Database", new ColumnMetadata { Alias = "Database", Description = "Database Name" }  },
                            { "Property", new ColumnMetadata { Alias = "Property", Description = "Extended property name" } },
                            { "Value", new ColumnMetadata { Alias = "Value", Description = "Extended property value" } },
                            { "ValidFrom", new ColumnMetadata { 
                                Alias = "Valid From", 
                                Description = "Date the extended property was set",
                                Link = new SystemDrillDownLinkColumnInfo {
                                    ReportFactory = () => DatabaseExtendedPropertiesHistoryReport.Instance,
                                    ColumnToParameterMap = new Dictionary<string, string> { 
                                        { "@DatabaseID", "DatabaseID" },
                                        { "@InstanceID", "InstanceID" },
                                        { "@Property", "Property" }
                                    }
                                }
                            } }
                        },
                        ResultName = "Extended Properties"
                    }
                }
            }
        };
    }
}
