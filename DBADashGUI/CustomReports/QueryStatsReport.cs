using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// The statement level workload, over the deltas the QueryStats collection stores.
    ///
    /// Opens on the family grain - one row per query shape - because that is how people think about a
    /// query.  Every literal variant of the same statement is one problem, in the same way that two hundred
    /// occurrences of one deadlock are one problem; an ad hoc query is stored that way to begin with, as one
    /// statement per shape, and shown as a template rather than as any one variant's text.  The statement
    /// count links down to the statements behind a family - the same query in several procedures - and a
    /// statement's plan count links down to the plan shapes it ran under, which is where a plan regression
    /// shows up as two rows instead of an inference.
    ///
    /// Two results rather than one.  The collection keeps the top families each interval and rolls the rest
    /// up, and a plan seen for the first time with an old compile time cannot be placed in the interval at
    /// all, so the second result carries what the first cannot say about itself: how much of the window is
    /// rolled up, how much could not be attributed, and whether any intervals were skipped.  A grid of
    /// query costs with no way to ask "is this the whole picture" is the thing worth not shipping.
    /// </summary>
    internal class QueryStatsReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ProcedureName = "QueryStats_Get",
            QualifiedProcedureName = "dbo.QueryStats_Get",
            ReportVisibilityRole = "public",
            ReportName = "Query Stats",
            Description =
                "Statement level resource usage aggregated from the plan cache, grouped by query shape.  Totals for the window are complete: what the collection does not keep in detail is rolled up rather than dropped.",
            SinglePageLayout = true,
            //  Puts the Trigger Collection button on the toolbar, so an instance that has just been switched
            //  on does not have to wait for the schedule to produce its first interval.
            TriggerCollectionTypes = new List<string> { DBADash.CollectionType.QueryStats.ToString() },
            SwitchTo = new ReportSwitch
            {
                ProcedureName = "QueryStatsCharts_Get",
                Text = "Show Charts",
                ToolTipText = "Switch to charts of the same data: over time, by object, statement and database",
                Image = Properties.Resources.StackedAreaChart
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Queries",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceID"] = new ColumnMetadata { Visible = false },
                        ["DatabaseID"] = new ColumnMetadata { Visible = false },
                        ["Instance"] = new ColumnMetadata { DisplayIndex = 1 },
                        ["Database"] = new ColumnMetadata { DisplayIndex = 2 },
                        ["QueryName"] = new ColumnMetadata
                        {
                            DisplayIndex = 3,
                            Alias = "Query",
                            Description =
                                "The object the statement belongs to, or a placeholder.  {Ad hoc} is a statement with no parent object: every literal variant of the query together, as one query shape.  {Other queries}, {Other variants} and {Other databases} are the rollups of what ranking did not keep in detail - their totals are real, only their breakdown is missing.  {Other databases} is the databases past the top N, combined.  Click an object to show its execution stats.  A query shape shared by statements in more than one object shows one of them and has no link: drill down to the statements to reach each object.",
                            Link = new ObjectExecutionLinkColumnInfo
                            {
                                // Not QueryName itself, which holds placeholders and, for a shape spanning
                                // several objects, just one of them
                                TargetColumn = "ObjectName",
                                DatabaseNameColumn = "Database"
                            }
                        },
                        ["ObjectName"] = new ColumnMetadata { Visible = false },
                        ["StatementText"] = new ColumnMetadata
                        {
                            DisplayIndex = 4,
                            Alias = "Statement",
                            Link = new TextLinkColumnInfo
                            {
                                TargetColumn = "StatementText",
                                TextHandling = SchemaCompare.CodeEditor.CodeEditorModes.SQL
                            },
                            Description =
                                "The statement itself, cut from the batch with its offsets.  Click to view.  An ad hoc query is shown as a template instead: one of its texts with the literal values replaced - '?', N'?', ? - and the comments removed, because those are what its variants differ in, and the figures are all of them together.  So is any row that includes one, such as its query shape.  A row of statements in procedures alone shows the heaviest one's text.  Blank means the text has not been collected yet - it is fetched separately from the statistics and capped per collection, so a new statement can show its numbers an interval or two before its text."
                        },
                        // The grid holds the id of the statement whose text is shown, not its batch, and the code
                        // viewer fetches the one batch that gets clicked.  See QueryStatementBatchTextLinkColumnInfo.
                        ["ViewBatch"] = new ColumnMetadata
                        {
                            DisplayIndex = 5,
                            Alias = "Batch",
                            Link = new QueryStatementBatchTextLinkColumnInfo { StatementIDColumn = "TextStatementID" },
                            Description = "The whole batch or object definition the statement came from.  Click to view.  For an ad hoc query it is an Example: the batch of one of the texts it ran with, with that execution's literal values, which other executions did not share - other values can cost far more or far less."
                        },
                        ["TextStatementID"] = new ColumnMetadata { Visible = false },
                        // Fetched when clicked, like the batch, and from the monitored instance where the collection
                        // did not capture it.  See QueryStatsPlanLinkColumnInfo.
                        ["ViewPlan"] = new ColumnMetadata
                        {
                            DisplayIndex = 6,
                            Alias = "Plan",
                            Link = new QueryStatsPlanLinkColumnInfo(),
                            Description = "The plan of the row's plan shape, where it ran under one - see Plan Hash.  Click to view.  View: captured with the statistics, or fetched since.  Example: an ad hoc query's, which is the plan of one of its texts - its operators are every text's that ran under the plan shape, but its literal values and estimates are that one's.  Find: not captured, so it is fetched from the plan cache on the monitored instance when clicked, and kept - which needs messaging enabled for the instance and the plan still cached.  Without messaging, a script that fetches it is shown instead.  The plan is the one the optimizer compiled, without the figures of any execution."
                        },
                        ["PlanStatementID"] = new ColumnMetadata { Visible = false },
                        ["RunningQueries"] = new ColumnMetadata
                        {
                            DisplayIndex = 7,
                            Alias = "Running Queries",
                            Link = new QueryHashRunningQueriesLinkColumnInfo(),
                            Description = "Executions with this query hash that the Running Queries collection caught in progress in the window, in this database: each with its actual text and literal values, its duration and its waits.  Snapshots catch whatever is running at the time, so they lean towards the long-running executions, which are the ones worth seeing.  The same query in a procedure has the same hash, so a shape shared with one shows both."
                        },
                        ["StatementCount"] = new ColumnMetadata
                        {
                            DisplayIndex = 8,
                            Alias = "Statements",
                            FormatString = "N0",
                            Description = "Distinct statements in this group.  More than one means the same query in several procedures, or in a procedure and in ad hoc SQL: the literal variants of an ad hoc query are one statement already - see Cache Entries.",
                            Link = new DrillDownLinkColumnInfo
                            {
                                DrillDownMode = DrillDownMode.ExistingWindow,
                                ReportProcedureName = "QueryStats_Get",
                                ColumnToParameterMap = new Dictionary<string, string>
                                {
                                    { "@QueryHash", "QueryHash" }
                                },
                                // Keeps the window and filters the family was read under, so the drill down
                                // opens the statements the row counted rather than every statement of that shape.
                                CarryUserParameters = true,
                                ChildPanelTitle = "Statements"
                            }
                        },
                        ["PlanCount"] = new ColumnMetadata
                        {
                            DisplayIndex = 9,
                            Alias = "Plans",
                            FormatString = "N0",
                            Description = "Distinct plan shapes.  A statement showing two in one window is the signature of a plan change.",
                            Link = new DrillDownLinkColumnInfo
                            {
                                DrillDownMode = DrillDownMode.ExistingWindow,
                                ReportProcedureName = "QueryStats_Get",
                                ColumnToParameterMap = new Dictionary<string, string>
                                {
                                    { "@StatementID", "StatementID" }
                                },
                                CarryUserParameters = true,
                                ChildPanelTitle = "Plans"
                            }
                        },
                        ["CacheEntries"] = new ColumnMetadata
                        {
                            DisplayIndex = 10,
                            Alias = "Cache Entries",
                            FormatString = "N0",
                            Description = "The most plan cache entries behind one plan of the row in a single interval.  One for a statement in a procedure.  An ad hoc query has an entry for each distinct text it ran with, so a large number is a query sending its values as literals, and the template shown stands for at least that many different texts."
                        },
                        ["Executions"] = new ColumnMetadata { DisplayIndex = 11, FormatString = "N0" },
                        ["CPUms"] = new ColumnMetadata
                        {
                            DisplayIndex = 12,
                            Alias = "CPU (ms)",
                            FormatString = "N0"
                        },
                        ["AvgCPUms"] = new ColumnMetadata
                        {
                            DisplayIndex = 13,
                            Alias = "Avg CPU (ms)",
                            FormatString = "N1"
                        },
                        ["DurationMs"] = new ColumnMetadata
                        {
                            DisplayIndex = 14,
                            Alias = "Duration (ms)",
                            FormatString = "N0"
                        },
                        ["AvgDurationMs"] = new ColumnMetadata
                        {
                            DisplayIndex = 15,
                            Alias = "Avg Duration (ms)",
                            FormatString = "N1"
                        },
                        ["PercentCPU"] = new ColumnMetadata
                        {
                            DisplayIndex = 16,
                            Alias = "CPU %",
                            FormatString = "N1",
                            Description = "Share of the CPU in what is shown, which unfiltered is the instance's whole window: the rollup rows are included, so the column sums to 100 rather than to whatever fraction the top N happens to cover.  In a drill down it is the share of the rows drilled into."
                        },
                        ["CPUmsPerSec"] = new ColumnMetadata
                        {
                            DisplayIndex = 17,
                            Alias = "CPU ms/sec",
                            FormatString = "N1",
                            Description = "Worker time per second of the time the collection actually covered, not of the window asked for.  Comparable between windows of different lengths and unaffected by intervals the service was not running for.  1000 is one core kept fully busy."
                        },
                        ["CPUmsPerSecPerCore"] = new ColumnMetadata
                        {
                            DisplayIndex = 18,
                            Alias = "CPU ms/sec/core",
                            FormatString = "N1",
                            Description = "The same figure divided by the instance's core count, so 100 means the whole box.  Blank where the core count is not known."
                        },
                        ["DurationMsPerSec"] = new ColumnMetadata
                        {
                            DisplayIndex = 19,
                            Alias = "Duration ms/sec",
                            FormatString = "N1",
                            Description = "Elapsed time per covered second.  Above 1000 means work overlapping itself: several executions running at once."
                        },
                        ["ExecutionsPerMin"] = new ColumnMetadata
                        {
                            DisplayIndex = 20,
                            Alias = "Executions/min",
                            FormatString = "N1"
                        },
                        ["LogicalReads"] = new ColumnMetadata { DisplayIndex = 21, Alias = "Logical Reads", FormatString = "N0" },
                        ["LogicalWrites"] = new ColumnMetadata { DisplayIndex = 22, Alias = "Logical Writes", FormatString = "N0" },
                        ["PhysicalReads"] = new ColumnMetadata { DisplayIndex = 23, Alias = "Physical Reads", FormatString = "N0" },
                        ["Spills"] = new ColumnMetadata
                        {
                            DisplayIndex = 24,
                            FormatString = "N0",
                            NullValue = "n/a",
                            Description = "Blank where the monitored instance predates SQL 2016, which is not the same as no spills."
                        },
                        ["AvgGrantKB"] = new ColumnMetadata
                        {
                            DisplayIndex = 25,
                            Alias = "Avg Grant (KB)",
                            FormatString = "N0",
                            NullValue = "n/a",
                            Description = "Memory grant per execution.  An execution that needed no grant counts as zero, so a group mixing plans that do and do not need one averages lower than any plan that does.  Blank where the monitored instance predates SQL 2016."
                        },
                        ["AvgUsedGrantKB"] = new ColumnMetadata
                        {
                            DisplayIndex = 26,
                            Alias = "Avg Used Grant (KB)",
                            FormatString = "N0",
                            NullValue = "n/a",
                            Description = "How much of the grant each execution actually used.  Blank where the monitored instance predates SQL 2016."
                        },
                        ["GrantUsedPercent"] = new ColumnMetadata
                        {
                            DisplayIndex = 27,
                            Alias = "Grant Used %",
                            FormatString = "N1",
                            Description = "Used grant as a share of the grant.  Low against a large grant is memory reserved and never touched, usually from an overestimate, and it can hold other queries in RESOURCE_SEMAPHORE waits.  High alongside spills means the grant was too small.  Blank where there was no grant or the instance predates SQL 2016."
                        },
                        ["Rows"] = new ColumnMetadata { DisplayIndex = 28, FormatString = "N0", NullValue = "n/a" },
                        ["IncludesRollup"] = new ColumnMetadata
                        {
                            DisplayIndex = 29,
                            Alias = "Rolled Up",
                            BooleanTrueValue = "Yes",
                            BooleanFalseValue = "",
                            Description = "The group's total includes work that was rolled up, so the figure is complete but its breakdown is not."
                        },
                        ["IsCompile"] = new ColumnMetadata
                        {
                            DisplayIndex = 30,
                            Alias = "Compiled",
                            BooleanTrueValue = "Yes",
                            BooleanFalseValue = "",
                            Description = "A plan compiled inside the window, so its counters were taken whole rather than diffed.  Always set for a query whose plan never stays cached."
                        },
                        ["QueryHash"] = new ColumnMetadata
                        {
                            DisplayIndex = 31,
                            Alias = "Query Hash",
                            Description = "The shape of the query, shared by every literal variant of it and comparable across instances of the same major version.  Also stored on running queries, so the same value links the two.  Click to look the query up in Query Store, which needs Query Store enabled on the database and messaging enabled for the instance.",
                            Link = new QueryStoreLinkColumnInfo
                            {
                                TargetColumn = "QueryHash",
                                TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.QueryHash,
                                InstanceIdColumn = "InstanceID",
                                DatabaseNameColumn = "Database"
                            }
                        },
                        ["PlanHash"] = new ColumnMetadata
                        {
                            DisplayIndex = 32,
                            Alias = "Plan Hash",
                            Description = "The plan shape, where the row ran under exactly one.  Blank means more than one, which the Plans column counts, or a rollup row that has no plan of its own.  Click to look the plan up in Query Store.  The Plan column opens the plan captured with the statistics.",
                            Link = new QueryStoreLinkColumnInfo
                            {
                                TargetColumn = "PlanHash",
                                TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.PlanHash,
                                InstanceIdColumn = "InstanceID",
                                DatabaseNameColumn = "Database"
                            }
                        },
                        ["StatementID"] = new ColumnMetadata { Visible = false },
                        ["FirstPeriodStart"] = new ColumnMetadata
                        {
                            DisplayIndex = 33,
                            Alias = "First Seen",
                            Description = "The start of the first collection interval in the window that the row ran in.  With Last Seen, the span its work was done in: it ran at some point in the first interval and the last, not necessarily at their edges."
                        },
                        ["LastSnapshotDate"] = new ColumnMetadata
                        {
                            DisplayIndex = 34,
                            Alias = "Last Seen",
                            Description = "The end of the last collection interval in the window that the row ran in - see First Seen."
                        }
                    }
                },
                [1] = new CustomReportResult
                {
                    ResultName = "Collection Quality",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["Instance"] = new ColumnMetadata { DisplayIndex = 1 },
                        ["Collections"] = new ColumnMetadata { DisplayIndex = 2, FormatString = "N0" },
                        ["SkippedCollections"] = new ColumnMetadata
                        {
                            DisplayIndex = 3,
                            Alias = "Skipped",
                            FormatString = "N0",
                            Description = "Intervals where the read was skipped because the previous one took too long.  The interval has no detail by design rather than by failure."
                        },
                        ["FirstCollections"] = new ColumnMetadata
                        {
                            DisplayIndex = 4,
                            Alias = "First Collections",
                            FormatString = "N0",
                            Description = "Intervals with no baseline to diff against - a service restart, or the collection being switched on.  Only plans compiled inside such an interval can be attributed to it."
                        },
                        ["CoveredMinutes"] = new ColumnMetadata
                        {
                            DisplayIndex = 5,
                            Alias = "Covered (min)",
                            FormatString = "N1",
                            Description = "How much of the window the collection actually covered.  Well below the window length means the service was not collecting for part of it."
                        },
                        ["WindowMinutes"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Window (min)", FormatString = "N1" },
                        ["PercentUnattributed"] = new ColumnMetadata
                        {
                            DisplayIndex = 7,
                            Alias = "Unattributed %",
                            FormatString = "N1",
                            Description = "Share of the window's CPU the collection could not attribute to a statement: plans seen for the first time whose compile time predates the interval, mostly in a first collection.  An estimate - their last execution, which finished inside the interval, plus their earlier work prorated over their time in cache."
                        },
                        ["UnattributedCPUms"] = new ColumnMetadata
                        {
                            DisplayIndex = 8,
                            Alias = "Unattributed CPU (ms)",
                            FormatString = "N0",
                            Description = "Estimated CPU in the window that could not be attributed to a statement - see Unattributed %."
                        },
                        ["UnattributedExecutions"] = new ColumnMetadata
                        {
                            DisplayIndex = 9,
                            Alias = "Unattributed Executions",
                            FormatString = "N0",
                            Description = "Estimated executions in the window that could not be attributed to a statement - see Unattributed %."
                        },
                        ["PercentRolledUp"] = new ColumnMetadata
                        {
                            DisplayIndex = 10,
                            Alias = "Rolled Up %",
                            FormatString = "N1",
                            Description = "Share of the window's CPU that is in a rollup row rather than named.  High means the top N is too small to describe this workload, not that anything was lost."
                        },
                        ["RolledUpCPUms"] = new ColumnMetadata { DisplayIndex = 11, Alias = "Rolled Up CPU (ms)", FormatString = "N0" },
                        ["BaselineEvictions"] = new ColumnMetadata
                        {
                            DisplayIndex = 12,
                            Alias = "Baseline Evictions",
                            FormatString = "N0",
                            Description = "Baseline rows dropped to stay within the configured cap.  Anything other than zero means the cap is costing attribution."
                        },
                        ["MaxReadDurationMs"] = new ColumnMetadata
                        {
                            DisplayIndex = 13,
                            Alias = "Max Read (ms)",
                            FormatString = "N0",
                            Description = "The longest read of sys.dm_exec_query_stats in the window.  This is the cost the collection puts on the monitored instance."
                        }
                    }
                }
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@InstanceID", ParamType = "INT" },
                    new() { ParamName = "@DatabaseID", ParamType = "INT" },
                    // Presence of @FromDate/@ToDate is what shows the global date filter on the report.
                    new() { ParamName = "@FromDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@ToDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@GroupBy", ParamType = "VARCHAR" },
                    // The drill down targets.  Supplying either one implies the grain below it, so a link
                    // needs nothing but a column to parameter mapping.
                    new() { ParamName = "@QueryHash", ParamType = "VARCHAR" },
                    new() { ParamName = "@StatementID", ParamType = "BIGINT" },
                    // Set when a chart slice is drilled into: an object name, or the label built from a
                    // statement's own text.  Also usable as a filter from the Parameters dialog.
                    new() { ParamName = "@ObjectName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@ExcludeRollups", ParamType = "BIT" },
                    new() { ParamName = "@Top", ParamType = "INT" }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@GroupBy",
                    Name = "Group By",
                    PickerItems = new Dictionary<object, string>
                    {
                        ["Family"] = "Query shape",
                        ["Statement"] = "Statement",
                        ["Plan"] = "Plan"
                    },
                    DefaultValue = "Family",
                    MenuBar = true,
                    DataType = typeof(string)
                },
                new()
                {
                    ParameterName = "@ExcludeRollups",
                    Name = "Rollups",
                    // Including them is the default and the honest one: the figures then add up to the work
                    // the instance did.  Named only answers a different question and should be read as a top
                    // N rather than as a total.
                    PickerItems = new Dictionary<object, string>
                    {
                        [false] = "Include",
                        [true] = "Named only"
                    },
                    DefaultValue = false,
                    MenuBar = true,
                    DataType = typeof(bool)
                },
                new()
                {
                    ParameterName = "@Top",
                    Name = "Top",
                    PickerItems = new Dictionary<object, string>
                    {
                        [25] = "Top 25",
                        [50] = "Top 50",
                        [100] = "Top 100",
                        [250] = "Top 250",
                        [1000] = "Top 1000"
                    },
                    DefaultValue = 100,
                    MenuBar = true,
                    DataType = typeof(int)
                }
            }
        };
    }
}
