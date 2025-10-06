using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    internal class DeletedDatabasesReport
    {
        public static SystemReport Instance => new()
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
    }
}