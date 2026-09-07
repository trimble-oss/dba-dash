using DBADashGUI.SchemaCompare;
using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// The deadlock report, over the graphs the Deadlocks collection stores.
    ///
    /// Opens on the signature summary - a deadlock that happens two hundred times is one problem, so
    /// "what deadlocks do we have" is a more useful first question than "what deadlocked at 14:07".
    /// Clicking the occurrence count narrows every result to that one pattern.
    ///
    /// The graph column links into the existing deadlock viewer, which is what replaces the
    /// sp_BlitzLock round trip: the graph comes from the repository rather than from a stored
    /// procedure the user has to deploy and wait for.  The grid carries the deadlock's key rather
    /// than its graph and fetches the one that is clicked, so a report over thousands of deadlocks
    /// doesn't hold every graph in memory to serve the handful anyone opens.
    /// </summary>
    internal class DeadlocksReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ProcedureName = "DeadlockSummary_Get",
            QualifiedProcedureName = "dbo.DeadlockSummary_Get",
            ReportVisibilityRole = "public",
            ReportName = "Deadlocks",
            SinglePageLayout = true,
            Description = "Deadlocks captured from extended events, grouped by signature so recurrences of one problem read as one problem.",
            //  Puts the Trigger Collection button on the toolbar.  That is the answer for an instance that
            //  isn't collecting: run the collection once on demand and the deadlocks the session still holds
            //  are stored and reported like any others - no second read path, and what is collected stays.
            TriggerCollectionTypes = new List<string> { "Deadlocks" },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Signatures",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["Signature"] = new ColumnMetadata { DisplayIndex = 1 },
                        ["Occurrences"] = new ColumnMetadata
                        {
                            DisplayIndex = 2,
                            Link = new DrillDownLinkColumnInfo
                            {
                                DrillDownMode = DrillDownMode.ExistingWindow,
                                ReportProcedureName = "DeadlockSummary_Get",
                                ColumnToParameterMap = new Dictionary<string, string>
                                {
                                    { "@Signature", "Signature" }
                                },
                                // Keeps the filters and the Processes setting the summary was read under,
                                // so the signature opens the occurrences the row counted rather than every
                                // occurrence of it.
                                CarryUserParameters = true
                            }
                        },
                        ["Victims"] = new ColumnMetadata { DisplayIndex = 3 },
                        ["Instances"] = new ColumnMetadata { DisplayIndex = 4 },
                        ["FirstSeen"] = new ColumnMetadata { DisplayIndex = 5, Alias = "First Seen" },
                        ["LastSeen"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Last Seen" },
                        ["ObjectNames"] = new ColumnMetadata { DisplayIndex = 7, Alias = "Objects" },
                        ["ProcedureNames"] = new ColumnMetadata { DisplayIndex = 8, Alias = "Procedures" },
                        ["IsParallel"] = new ColumnMetadata { DisplayIndex = 9, Alias = "Parallel" },
                        ["HasAnalysis"] = new ColumnMetadata { DisplayIndex = 10, Alias = "Analysed" },
                        // Shown when the collection is disabled; the proc returns this instead of the summary.
                        ["Message"] = new ColumnMetadata()
                    }
                },
                [1] = new CustomReportResult
                {
                    ResultName = "Grouped",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["GroupValue"] = new ColumnMetadata { DisplayIndex = 1, Alias = "Value" },
                        ["Deadlocks"] = new ColumnMetadata
                        {
                            DisplayIndex = 2,
                            // All five filter parameters are mapped; the proc populates only the column for the
                            // dimension currently selected and the framework skips nulls, so one map serves
                            // whichever way the picker is grouping.
                            Link = new DrillDownLinkColumnInfo
                            {
                                DrillDownMode = DrillDownMode.ExistingWindow,
                                ReportProcedureName = "DeadlockSummary_Get",
                                ColumnToParameterMap = new Dictionary<string, string>
                                {
                                    { "@ApplicationName", "FilterApplication" },
                                    { "@DatabaseName", "FilterDatabase" },
                                    { "@LoginName", "FilterLogin" },
                                    { "@HostName", "FilterHost" },
                                    { "@ProcedureName", "FilterProcedure" }
                                },
                                // Victims Only in particular: the grouped counts are of victim processes
                                // when it is set, and the drill-down has to be read the same way.
                                CarryUserParameters = true
                            }
                        },
                        ["VictimProcesses"] = new ColumnMetadata { DisplayIndex = 3, Alias = "Victim Processes" },
                        ["Processes"] = new ColumnMetadata { DisplayIndex = 4 },
                        ["FirstSeen"] = new ColumnMetadata { DisplayIndex = 5, Alias = "First Seen" },
                        ["LastSeen"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Last Seen" },
                        ["FilterApplication"] = new ColumnMetadata { Visible = false },
                        ["FilterDatabase"] = new ColumnMetadata { Visible = false },
                        ["FilterLogin"] = new ColumnMetadata { Visible = false },
                        ["FilterHost"] = new ColumnMetadata { Visible = false },
                        ["FilterProcedure"] = new ColumnMetadata { Visible = false }
                    }
                },
                [2] = new CustomReportResult
                {
                    ResultName = "Deadlocks",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceID"] = new ColumnMetadata { Visible = false },
                        // The number this deadlock is given in the Participants result too, so a row here
                        // can be matched to the processes that took part in it.
                        ["DeadlockGroup"] = new ColumnMetadata { DisplayIndex = 1, Alias = "Deadlock #" },
                        ["InstanceDisplayName"] = new ColumnMetadata { DisplayIndex = 2, Alias = "Instance" },
                        ["EventTime"] = new ColumnMetadata { DisplayIndex = 3, Alias = "Event Time" },
                        // Synthetic column - the grid holds the key, not the graph, and the viewer fetches
                        // the one graph that gets clicked.  See DeadlockGraphLookupLinkColumnInfo.
                        ["ViewDeadlock"] = new ColumnMetadata
                        {
                            DisplayIndex = 4,
                            Alias = "Graph",
                            Description = "Open this deadlock in the deadlock viewer",
                            Link = new DeadlockGraphLookupLinkColumnInfo()
                        },
                        ["DeadlockHash"] = new ColumnMetadata { Visible = false },
                        // The module name is three part - the database is the module's own, which is not
                        // necessarily the database the victim was connected to.  See ObjectExecutionLinkColumnInfo.
                        ["VictimProcedure"] = new ColumnMetadata
                        {
                            DisplayIndex = 5,
                            Alias = "Victim Procedure",
                            Description = "Click to show execution stats for this procedure",
                            Link = new ObjectExecutionLinkColumnInfo
                            {
                                TargetColumn = "VictimProcedure",
                                DatabaseNameColumn = "VictimDatabase"
                            }
                        },
                        ["VictimStatement"] = new ColumnMetadata
                        {
                            DisplayIndex = 6,
                            Alias = "Victim Statement",
                            Link = new TextLinkColumnInfo
                            {
                                TargetColumn = "VictimStatement",
                                TextHandling = CodeEditor.CodeEditorModes.SQL
                            }
                        },
                        ["VictimDatabase"] = new ColumnMetadata { DisplayIndex = 7, Alias = "Victim Database" },
                        ["VictimApp"] = new ColumnMetadata { DisplayIndex = 8, Alias = "Victim Application" },
                        ["VictimLogin"] = new ColumnMetadata { DisplayIndex = 9, Alias = "Victim Login" },
                        ["VictimHost"] = new ColumnMetadata { DisplayIndex = 10, Alias = "Victim Host" },
                        ["ObjectNames"] = new ColumnMetadata { DisplayIndex = 11, Alias = "Objects" },
                        ["Signature"] = new ColumnMetadata { DisplayIndex = 12 },
                        ["ProcessCount"] = new ColumnMetadata { DisplayIndex = 13, Alias = "Processes" },
                        ["VictimCount"] = new ColumnMetadata { DisplayIndex = 14, Alias = "Victims" },
                        // Processes minus victims.  On a parallel deadlock the process list is one entry
                        // per worker thread, so Sessions is the figure that means what people expect.
                        ["Survivors"] = new ColumnMetadata { DisplayIndex = 15 },
                        ["Sessions"] = new ColumnMetadata { DisplayIndex = 16 },
                        ["ResourceCount"] = new ColumnMetadata { DisplayIndex = 17, Alias = "Resources" },
                        ["IsParallel"] = new ColumnMetadata { DisplayIndex = 18, Alias = "Parallel" }
                    }
                },
                [3] = new CustomReportResult
                {
                    ResultName = "Participants",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceID"] = new ColumnMetadata { Visible = false },
                        // Leads the grid because this result has several rows per deadlock: it is what
                        // shows where one deadlock's processes end and the next one's begin, and it is
                        // the same number the Deadlocks result gave that deadlock.
                        ["DeadlockGroup"] = new ColumnMetadata { DisplayIndex = 1, Alias = "Deadlock #" },
                        ["InstanceDisplayName"] = new ColumnMetadata { DisplayIndex = 2, Alias = "Instance" },
                        ["EventTime"] = new ColumnMetadata { DisplayIndex = 3, Alias = "Event Time" },
                        // The same viewer link as the Deadlocks result, so the graph can be opened from the
                        // process that is being read rather than by going back to find the occurrence row.
                        ["ViewDeadlock"] = new ColumnMetadata
                        {
                            DisplayIndex = 4,
                            Alias = "Graph",
                            Description = "Open this deadlock in the deadlock viewer",
                            Link = new DeadlockGraphLookupLinkColumnInfo()
                        },
                        ["DeadlockHash"] = new ColumnMetadata { Visible = false },
                        ["IsVictim"] = new ColumnMetadata { DisplayIndex = 5, Alias = "Victim" },
                        ["DatabaseName"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Database" },
                        ["ProcedureName"] = new ColumnMetadata
                        {
                            DisplayIndex = 7,
                            Alias = "Procedure",
                            Description = "Click to show execution stats for this procedure",
                            Link = new ObjectExecutionLinkColumnInfo
                            {
                                TargetColumn = "ProcedureName",
                                DatabaseNameColumn = "DatabaseName"
                            }
                        },
                        ["StatementText"] = new ColumnMetadata
                        {
                            DisplayIndex = 8,
                            Alias = "Statement",
                            Link = new TextLinkColumnInfo
                            {
                                TargetColumn = "StatementText",
                                TextHandling = CodeEditor.CodeEditorModes.SQL
                            }
                        },
                        ["ClientApp"] = new ColumnMetadata { DisplayIndex = 9, Alias = "Application" },
                        ["LoginName"] = new ColumnMetadata { DisplayIndex = 10, Alias = "Login" },
                        ["HostName"] = new ColumnMetadata { DisplayIndex = 11, Alias = "Host" },
                        ["LockMode"] = new ColumnMetadata { DisplayIndex = 12, Alias = "Lock Mode" },
                        ["WaitResource"] = new ColumnMetadata { DisplayIndex = 13, Alias = "Wait Resource" },
                        ["WaitTimeMs"] = new ColumnMetadata { DisplayIndex = 14, Alias = "Wait Time (ms)" },
                        ["LogUsed"] = new ColumnMetadata { DisplayIndex = 15, Alias = "Log Used" },
                        ["IsolationLevel"] = new ColumnMetadata { DisplayIndex = 16, Alias = "Isolation Level" },
                        ["TransactionName"] = new ColumnMetadata { DisplayIndex = 17, Alias = "Transaction" },
                        ["TransactionCount"] = new ColumnMetadata { DisplayIndex = 18, Alias = "Tran Count" },
                        ["Priority"] = new ColumnMetadata { DisplayIndex = 19 },
                        ["Status"] = new ColumnMetadata { DisplayIndex = 20 },
                        ["SPID"] = new ColumnMetadata { DisplayIndex = 21 },
                        ["Ecid"] = new ColumnMetadata { DisplayIndex = 22 },
                        ["HostPid"] = new ColumnMetadata { DisplayIndex = 23, Alias = "Host PID" },
                        ["LastTransactionStarted"] = new ColumnMetadata { DisplayIndex = 24, Alias = "Tran Started" },
                        ["LastBatchStarted"] = new ColumnMetadata { DisplayIndex = 25, Alias = "Batch Started" },
                        ["LastBatchCompleted"] = new ColumnMetadata { DisplayIndex = 26, Alias = "Batch Completed" },
                        ["InputBuffer"] = new ColumnMetadata
                        {
                            DisplayIndex = 27,
                            Alias = "Input Buffer",
                            Link = new TextLinkColumnInfo
                            {
                                TargetColumn = "InputBuffer",
                                TextHandling = CodeEditor.CodeEditorModes.SQL
                            }
                        },
                        // The decoded SET options, as sp_BlitzLock reports them.
                        ["ClientOptions1"] = new ColumnMetadata { DisplayIndex = 28, Alias = "Client Options 1", Visible = true },
                        ["ClientOptions2"] = new ColumnMetadata { DisplayIndex = 29, Alias = "Client Options 2", Visible = true },
                        // The raw bitmasks they were decoded from.  Hidden by default; still exportable.
                        ["ClientOption1"] = new ColumnMetadata { DisplayIndex = 30, Alias = "Client Option 1 (raw)", Visible = false },
                        ["ClientOption2"] = new ColumnMetadata { DisplayIndex = 31, Alias = "Client Option 2 (raw)", Visible = false },
                        ["ProcessIndex"] = new ColumnMetadata { Visible = false },
                        ["Signature"] = new ColumnMetadata { Visible = false }
                    }
                }
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@InstanceID", ParamType = "INT" },
                    // Presence of @FromDate/@ToDate is what shows the global date filter on the report.
                    new() { ParamName = "@FromDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@ToDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@Signature", ParamType = "VARCHAR" },
                    new() { ParamName = "@GroupBy", ParamType = "VARCHAR" },
                    // Filters.  Editable from the Parameters dialog, and the targets the Grouped result
                    // drills into.
                    new() { ParamName = "@ApplicationName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@DatabaseName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@LoginName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@HostName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@ProcedureName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@ObjectName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@VictimsOnly", ParamType = "BIT" }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@GroupBy",
                    Name = "Group By",
                    // The dimensions are columns on dbo.DeadlockProcesses, so one picker replaces what would
                    // otherwise be five near-identical reports.
                    PickerItems = new Dictionary<object, string>
                    {
                        ["Application"] = "Application",
                        ["Database"] = "Database",
                        ["Login"] = "Login",
                        ["Host"] = "Host",
                        ["Procedure"] = "Procedure"
                    },
                    DefaultValue = "Application",
                    MenuBar = true,
                    DataType = typeof(string)
                },
                new()
                {
                    ParameterName = "@VictimsOnly",
                    // Named for what it scopes rather than as a yes/no: it selects which processes the
                    // Grouped and Participants results count and list, and narrows the other filters to
                    // the victim.  It does not select deadlocks - every deadlock has a victim.
                    Name = "Processes",
                    PickerItems = new Dictionary<object, string>
                    {
                        [false] = "All involved",
                        [true] = "Victims only"
                    },
                    DefaultValue = false,
                    MenuBar = true,
                    DataType = typeof(bool)
                }
            }
        };
    }
}
