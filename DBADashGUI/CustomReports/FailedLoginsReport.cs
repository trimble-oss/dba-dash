using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.CustomReports
{
    internal class FailedLoginsReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ProcedureName = "FailedLogins_Get",
            QualifiedProcedureName = "dbo.FailedLogins_Get",
            ReportVisibilityRole = "public",
            ReportName = "Failed Logins",
            TriggerCollectionTypes = new List<string>() { "FailedLogins"},
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Server Summary",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new ColumnMetadata
                        {
                            Alias = "Instance"
                        },
                        ["FailedLogins"] = new ColumnMetadata
                        {
                            DisplayIndex = 2,
                            Alias = "Failed Logins",
                            Link = new DrillDownLinkColumnInfo()
                            {
                                ColumnToParameterMap = new Dictionary<string, string>
                                {
                                    { "@InstanceID", "InstanceID" }
                                },
                                ReportProcedureName = "FailedLogins_Get"
                            }
                        },
                        ["FirstFailedLogin"] = new ColumnMetadata
                        {
                            DisplayIndex = 3,
                            Alias = "First Failed Login"
                        },
                        ["LastFailedLogin"] = new ColumnMetadata
                        {
                            DisplayIndex = 4,
                            Alias = "Last Failed Login"
                        },
                        ["SnapshotAge"] = new ColumnMetadata
                        {
                            DisplayIndex = 5,
                            Alias = "Snapshot Age",
                            Highlighting = new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true }
                        },
                        ["SnapshotDate"] = new ColumnMetadata
                        {
                            DisplayIndex = 6,
                            Alias = "Snapshot Date",
                            Highlighting = new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true }
                        },
                        ["SnapshotStatus"] = new ColumnMetadata
                        {
                            Visible = false,
                            DisplayIndex = 7
                        },
                        ["InstanceID"] = new ColumnMetadata
                        {
                            DisplayIndex = 1,
                            Visible = false
                        },
                        ["Message"] = new ColumnMetadata(),
                        ["Url"] = new ColumnMetadata()
                        {
                            Link = new UrlLinkColumnInfo()
                            {
                                TargetColumn = "Url"
                            }
                        }
                    }
                },
                [1] = new CustomReportResult
                {
                    ResultName = "Login Summary",
                    Columns = new Dictionary<string, ColumnMetadata>()
                },
                [2] = new CustomReportResult
                {
                    ResultName = "Client Summary",
                    Columns = new Dictionary<string, ColumnMetadata>()
                },
                [3] = new CustomReportResult
                {
                    ResultName = "Message Summary",
                    Columns = new Dictionary<string, ColumnMetadata>()
                },
                [4] = new CustomReportResult
                {
                    ResultName = "Detail",
                    Columns = new Dictionary<string, ColumnMetadata>()
                    {
                        ["LogDate"] = new ColumnMetadata
                        {
                            Alias = "Log Date"
                        },
                        ["Text"] = new ColumnMetadata()
                    }
                }
            },
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
                        ParamName = "@InstanceID",
                        ParamType = "INT"
                    }
                },
            },
            Pickers = new List<Picker>
            {
                new Picker
                {
                    ParameterName = "@Days",
                    Name = "Days",
                    PickerItems = new Dictionary<object, string>
                    {
                        [1] = "1 Day",
                        [2] = "2 Days",
                        [3] = "3 Days",
                        [4] = "4 Days",
                        [5] = "5 Days",
                        [6] = "6 Days",
                        [7] = "7 Days",
                        [14] = "14 Days",
                        [30] = "30 Days",
                        [60] = "60 Days",
                        [90] = "90 Days"
                    },
                    DefaultValue = 2,
                    MenuBar = true,
                    DataType = typeof(int)
                }
            }
        };
    }
}
