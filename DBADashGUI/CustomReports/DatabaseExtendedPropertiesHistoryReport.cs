using System;
using System.Collections.Generic;
using System.Text;

namespace DBADashGUI.CustomReports
{
    internal class DatabaseExtendedPropertiesHistoryReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            QualifiedProcedureName = "dbo.DatabaseExtendedPropertiesHistory_Get",
            ProcedureName = "DatabaseExtendedPropertiesHistory_Get",
            ReportName = "Database Extended Properties History",
            Description = "Database Extended Properties History",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new()
                    {
                        ParamName = "@DatabaseID",
                        ParamType = "INT"
                    },
                    new()
                    {
                        ParamName = "@InstanceID",
                        ParamType = "INT"
                    },
                    new()
                    {
                        ParamName = "@Property",
                        ParamType = "NVARCHAR"
                    },
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "Database", new ColumnMetadata { Alias = "Database", Description = "Database Name" }  },
                            { "Property", new ColumnMetadata { Alias = "Property", Description = "Extended property name" } },
                            { "OldValue", new ColumnMetadata { Alias = "Old Value", Description = "Old Extended property value" } },
                            { "NewValue", new ColumnMetadata { Alias = "New Value", Description = "New Extended property value" } },
                            { "ValidFrom", new ColumnMetadata { Alias = "Valid From", Description = "Date the old value is effective from" } },
                            { "ValidTo", new ColumnMetadata { Alias = "Valid To", Description = "Date the old value is effective to" } }
                        },
                        ResultName = "Extended Properties History"
                    }
                }
            }
        };
    }
}
