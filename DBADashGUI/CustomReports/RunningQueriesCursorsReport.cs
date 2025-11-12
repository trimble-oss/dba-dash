using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    internal class RunningQueriesCursorsReport
    {
        public static SystemReport Instance => new()
        {
            ReportName = "Running Queries - Cursors",
            SchemaName = "dbo",
            ProcedureName = "RunningQueriesCursors_Get",
            QualifiedProcedureName = "dbo.RunningQueriesCursors_Get",
            CanEditReport = false,
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Cursors",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "InstanceID", new ColumnMetadata {
                                Alias = "Instance ID",
                                Visible = false,
                                Description = "InstanceID in DBA Dash repository database (dbo.Instances table)"
                            }},
                            { "SnapshotDateUTC", new ColumnMetadata {
                                Alias = "Snapshot Date",
                                Description = "Date and time of the collection"
                            }},
                            { "session_id", new ColumnMetadata {
                                Alias = "Session ID",
                                Description = "session_id/SPID"
                            }},
                            { "name", new ColumnMetadata {
                                Alias = "Name",
                                Description = "Name of cursor as defined by the user"
                            }},
                            { "text", new ColumnMetadata {
                                Alias = "Text",
                                Description = "Full text associated with the sql_handle for this cursor",
                                Link = new TextLinkColumnInfo() { TargetColumn = "text", TextHandling = SchemaCompare.CodeEditor.CodeEditorModes.SQL}
                            }},
                            { "statement", new ColumnMetadata {
                                Alias = "Statement",
                                Description = "Text associated with the sql_handle for this cursor with statement start/end offset applied.",
                                Link = new TextLinkColumnInfo() { TargetColumn = "statement", TextHandling = SchemaCompare.CodeEditor.CodeEditorModes.SQL}
                            }},
                            { "sql_handle", new ColumnMetadata {
                                Alias = "SQL Handle",
                                Description = "Handle associated with the batch text that declared the cursor",
                                Visible = false
                            }},
                            { "statement_start_offset", new ColumnMetadata {
                                Alias = "Statement Start Offset",
                                Description = "Position within the SQL batch where the cursor statement starts",
                                Visible = false
                            }},
                            { "statement_end_offset", new ColumnMetadata {
                                Alias = "Statement End Offset",
                                Description = "Position within the SQL batch where the cursor statement ends",
                                Visible = false
                            }},
                            { "plan_generation_num", new ColumnMetadata {
                                Alias = "Plan Generation #",
                                Description = "A sequence number that can be used to distinguish between instances of plans following compilation"
                            }},
                            { "creation_time_utc", new ColumnMetadata {
                                Alias = "Creation Time",
                                Description = "Timestamp when the cursor was created"
                            }},
                            { "is_open", new ColumnMetadata {
                                Alias = "Is Open",
                                Description = "Indicates whether the cursor is open"
                            }},
                            { "is_async_population", new ColumnMetadata {
                                Alias = "Is Async Population",
                                Description = "Specifies whether the background thread is still asynchronously populating a KEYSET or STATIC cursor"
                            }},
                            { "is_close_on_commit", new ColumnMetadata {
                                Alias = "Is Close On Commit",
                                Description = "Indicates whether the cursor will close when the transaction ends"
                            }},
                            { "fetch_status", new ColumnMetadata {
                                Alias = "Fetch Status (int)",
                                Description = "Returns last fetch status of the cursor",
                                Visible = false
                            }},
                            { "fetch_status_desc", new ColumnMetadata {
                                Alias = "Fetch Status",
                                Description = "Returns last fetch status of the cursor"
                            }},
                            { "fetch_buffer_size", new ColumnMetadata {
                                Alias = "Fetch Buffer Size",
                                Description = "Returns the size of the fetch buffer. 1 for T-SQL cursors.  Can be higher for API cursors."
                            }},
                            { "fetch_buffer_start", new ColumnMetadata {
                                Alias = "Fetch Buffer Start",
                                Description = "For FAST_FORWARD and DYNAMIC cursors, it returns 0 if the cursor is not open or if it is positioned before the first row. Otherwise, it returns -1.\n\nFor STATIC and KEYSET cursors, it returns 0 if the cursor is not open, and -1 if the cursor is positioned beyond the last row.\n\nOtherwise, it returns the row number in which it is positioned."
                            }},
                            { "ansi_position", new ColumnMetadata {
                                Alias = "ANSI Position",
                                Description = "Cursor position within the fetch buffer."
                            }},
                            { "worker_time_ms", new ColumnMetadata {
                                Alias = "Worker Time (ms)",
                                Description = "Time spent, in milliseconds, by the workers executing this cursor.",
                                FormatString = "N3"
                            }},
                            { "worker_time", new ColumnMetadata {
                                Alias = "Worker Time",
                                Description = "Time spent by the workers executing this cursor.",
                            }},
                            { "reads", new ColumnMetadata {
                                Alias = "Reads",
                                Description = "Number of reads performed by the cursor",
                                FormatString = "N0"
                            }},
                            { "writes", new ColumnMetadata {
                                Alias = "Writes",
                                Description = "Number of writes performed by the cursor",
                                FormatString = "N0"
                            }},
                            { "dormant_duration_ms", new ColumnMetadata {
                                Alias = "Dormant Duration (ms)",
                                Description = "Milliseconds since the last query (open or fetch) on this cursor was started",
                                FormatString = "N0",
                                Visible = false
                            }},
                            { "dormant_duration", new ColumnMetadata {
                                Alias = "Dormant Duration",
                                Description = "Time since the last query (open or fetch) on this cursor was started"
                            }},
                        }
                    }
                }
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new()
                    {
                        ParamName = "@InstanceID",
                        ParamType = "INT"
                    },
                    new()
                    {
                        ParamName = "@SessionID",
                        ParamType = "INT"
                    },
                    new()
                    {
                        ParamName = "@SnapshotDateUTC",
                        ParamType = "DATETIME2"
                    }
                },
            }
        };
    }
}