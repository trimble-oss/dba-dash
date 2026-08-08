using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Audit report of sessions killed from DBA Dash (who killed what, and the outcome).
    /// Visible only to users who can kill sessions (members of the AllowKillSession role, or db_owner/admin).
    /// </summary>
    internal class KillSessionLogReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ProcedureName = "KillSessionLog_Get",
            QualifiedProcedureName = "dbo.KillSessionLog_Get",
            ReportVisibilityRole = "AllowKillSession",
            ReportName = "Killed Sessions",
            Description = "Audit log of sessions killed from DBA Dash - who killed what, and the outcome.",
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
                    ResultName = "Killed Sessions",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceID"] = new ColumnMetadata { Visible = false },
                        ["InstanceGroupName"] = new ColumnMetadata
                        {
                            Alias = "Instance",
                            DisplayIndex = 1,
                            Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "" },
                            Description = "Instance the session was killed on. Click to navigate to the instance."
                        },
                        ["log_date"] = new ColumnMetadata { Alias = "Killed At", DisplayIndex = 2 },
                        ["killed_by"] = new ColumnMetadata { Alias = "Killed By", DisplayIndex = 3 },
                        ["session_id"] = new ColumnMetadata
                        {
                            Alias = "Session ID",
                            DisplayIndex = 4,
                            Link = new RunningQueriesSessionDetailLinkColumnInfo(),
                            Description = "Click to open the Running Queries session detail for this session in the snapshot the kill was actioned from."
                        },
                        ["login_name"] = new ColumnMetadata { Alias = "Login", DisplayIndex = 5 },
                        ["host_name"] = new ColumnMetadata { Alias = "Host", DisplayIndex = 6 },
                        ["program_name"] = new ColumnMetadata { Alias = "Program", DisplayIndex = 7 },
                        ["database_name"] = new ColumnMetadata { Alias = "Database", DisplayIndex = 8 },
                        ["command"] = new ColumnMetadata { Alias = "Command", DisplayIndex = 9 },
                        ["session_status"] = new ColumnMetadata { Alias = "Session Status", DisplayIndex = 10 },
                        ["BlockCountRecursive"] = new ColumnMetadata
                        {
                            Alias = "Blocked Count Recursive",
                            DisplayIndex = 11,
                            Description = "Number of sessions this session was blocking (directly or indirectly) at the time of the snapshot."
                        },
                        ["blocking_session_id"] = new ColumnMetadata { Alias = "Blocking Session ID", DisplayIndex = 12 },
                        ["status"] = new ColumnMetadata { Alias = "Outcome", DisplayIndex = 13 },
                        ["SnapshotDate"] = new ColumnMetadata
                        {
                            Alias = "Snapshot Date",
                            DisplayIndex = 14,
                            Link = new RunningQueriesLinkColumnInfo(),
                            Description = "Click to open the full Running Queries snapshot (all sessions) this kill was actioned from."
                        },
                        ["request_start_time"] = new ColumnMetadata { Alias = "Request Start", DisplayIndex = 15 },
                        // text / batch_text are joined from the RunningQueries snapshot. Click to view in the SQL viewer.
                        ["text"] = new ColumnMetadata
                        {
                            Alias = "Text",
                            DisplayIndex = 16,
                            Link = new TextLinkColumnInfo { TargetColumn = "text", TextHandling = SchemaCompare.CodeEditor.CodeEditorModes.SQL },
                            Description = "The statement that was running (joined from the RunningQueries snapshot). Click to view."
                        },
                        ["batch_text"] = new ColumnMetadata
                        {
                            Alias = "Batch Text",
                            DisplayIndex = 17,
                            Link = new TextLinkColumnInfo { TargetColumn = "batch_text", TextHandling = SchemaCompare.CodeEditor.CodeEditorModes.SQL },
                            Description = "The full batch text (joined from the RunningQueries snapshot). Click to view."
                        },
                        ["MessageGroupID"] = new ColumnMetadata { Visible = false }
                    }
                }
            }
        };
    }
}
