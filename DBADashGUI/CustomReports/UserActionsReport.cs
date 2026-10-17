using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Audit report of changes users have made from DBA Dash (dbo.UserActionLog) - who did what, to which instance, and the
    /// outcome.  Currently collection schedule changes and pauses, so visible only to users who can make them (members of
    /// the AllowScheduleChanges role, or db_owner/admin).
    /// </summary>
    internal class UserActionsReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ProcedureName = "UserActionLog_Get",
            QualifiedProcedureName = "dbo.UserActionLog_Get",
            ReportVisibilityRole = "AllowScheduleChanges",
            ReportName = "User Actions",
            Description = "Audit log of changes made from DBA Dash, such as collection schedule changes and pauses - who did what, and the outcome.",
            CanEditReport = false,
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@Days", ParamType = "INT" }
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
                        [1] = "1 Day",
                        [2] = "2 Days",
                        [7] = "7 Days",
                        [14] = "14 Days",
                        [30] = "30 Days",
                        [60] = "60 Days",
                        [90] = "90 Days",
                        [180] = "180 Days",
                        [365] = "365 Days"
                    },
                    DefaultValue = 7,
                    MenuBar = true,
                    DataType = typeof(int)
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "User Actions",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["log_date"] = new ColumnMetadata { Alias = "Date", DisplayIndex = 0 },
                        ["user_name"] = new ColumnMetadata { Alias = "User", DisplayIndex = 1, Description = "Windows user running the GUI." },
                        ["login_name"] = new ColumnMetadata { Alias = "Login", DisplayIndex = 2, Description = "Login used to connect to the repository." },
                        ["action_type"] = new ColumnMetadata { Alias = "Action", DisplayIndex = 3 },
                        ["InstanceID"] = new ColumnMetadata { Visible = false },
                        ["InstanceGroupName"] = new ColumnMetadata
                        {
                            Alias = "Instance",
                            DisplayIndex = 4,
                            Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "" },
                            Description = "Instance the action applied to - blank for a change to a service, which applies to each of its instances.  Click to navigate to the instance."
                        },
                        ["database_name"] = new ColumnMetadata { Alias = "Database", DisplayIndex = 5 },
                        ["service_name"] = new ColumnMetadata { Alias = "Service", DisplayIndex = 6, Description = "The DBA Dash service the request was sent to." },
                        ["details"] = new ColumnMetadata
                        {
                            Alias = "Details",
                            DisplayIndex = 7,
                            Link = new TextLinkColumnInfo { TargetColumn = "details", TextHandling = SchemaCompare.CodeEditor.CodeEditorModes.Json },
                            Description = "What was changed (JSON).  Click to view."
                        },
                        ["status"] = new ColumnMetadata { Alias = "Outcome", DisplayIndex = 8 },
                        ["MessageGroupID"] = new ColumnMetadata
                        {
                            Alias = "Message Group ID",
                            DisplayIndex = 9,
                            Visible = false,
                            Description = "Identifies the message sent to the service - matches the service's own log, for troubleshooting."
                        }
                    }
                }
            }
        };
    }
}
