using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Audit report of query plans flushed from the cache from DBA Dash (who flushed what, and the outcome).
    /// Visible only to users who can flush plans (members of the AllowPlanForcing role, or db_owner/admin).
    /// </summary>
    internal class FlushPlanLogReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ProcedureName = "FlushPlanLog_Get",
            QualifiedProcedureName = "dbo.FlushPlanLog_Get",
            ReportVisibilityRole = "AllowPlanForcing",
            ReportName = "Flushed Plans",
            Description = "Audit log of query plans flushed from the plan cache from DBA Dash - who flushed what, and the outcome.",
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
                    ResultName = "Flushed Plans",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceID"] = new ColumnMetadata { Visible = false },
                        ["InstanceGroupName"] = new ColumnMetadata
                        {
                            Alias = "Instance",
                            DisplayIndex = 1,
                            Link = new NavigateTreeLinkColumnInfo { InstanceColumn = "InstanceID", DatabaseColumn = "" },
                            Description = "Instance the plan was flushed on. Click to navigate to the instance."
                        },
                        ["log_date"] = new ColumnMetadata { Alias = "Flushed At", DisplayIndex = 2 },
                        ["flushed_by"] = new ColumnMetadata { Alias = "Flushed By", DisplayIndex = 3 },
                        ["session_id"] = new ColumnMetadata
                        {
                            Alias = "Session ID",
                            DisplayIndex = 4,
                            Link = new RunningQueriesSessionDetailLinkColumnInfo(),
                            Description = "Click to open the Running Queries session detail for this session in the snapshot the flush was actioned from."
                        },
                        ["login_name"] = new ColumnMetadata { Alias = "Login", DisplayIndex = 5 },
                        ["host_name"] = new ColumnMetadata { Alias = "Host", DisplayIndex = 6 },
                        ["program_name"] = new ColumnMetadata { Alias = "Program", DisplayIndex = 7 },
                        ["database_name"] = new ColumnMetadata { Alias = "Database", DisplayIndex = 8 },
                        ["command"] = new ColumnMetadata { Alias = "Command", DisplayIndex = 9 },
                        ["session_status"] = new ColumnMetadata { Alias = "Session Status", DisplayIndex = 10 },
                        ["plan_handle"] = new ColumnMetadata { Alias = "Plan Handle", DisplayIndex = 11 },
                        ["status"] = new ColumnMetadata { Alias = "Outcome", DisplayIndex = 12 },
                        ["SnapshotDate"] = new ColumnMetadata
                        {
                            Alias = "Snapshot Date",
                            DisplayIndex = 13,
                            Link = new RunningQueriesLinkColumnInfo(),
                            Description = "Click to open the full Running Queries snapshot (all sessions) this flush was actioned from."
                        },
                        ["request_start_time"] = new ColumnMetadata { Alias = "Request Start", DisplayIndex = 14 },
                        // text / batch_text are joined from the RunningQueries snapshot. Click to view in the SQL viewer.
                        ["text"] = new ColumnMetadata
                        {
                            Alias = "Text",
                            DisplayIndex = 15,
                            Link = new TextLinkColumnInfo { TargetColumn = "text", TextHandling = SchemaCompare.CodeEditor.CodeEditorModes.SQL },
                            Description = "The statement that was running (joined from the RunningQueries snapshot). Click to view."
                        },
                        ["batch_text"] = new ColumnMetadata
                        {
                            Alias = "Batch Text",
                            DisplayIndex = 16,
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
