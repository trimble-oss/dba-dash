using DBADashGUI.Charts;
using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// The chart view of the deadlock data - the shape of the problem rather than its rows.
    ///
    /// Every grouping is shown at once rather than behind a picker: the question these charts answer is
    /// which dimension explains the deadlocks, and that comparison cannot be made one pie at a time.
    /// The Victims Only toggle switches all six between "who is losing" and "who is involved", which are
    /// different questions - a busy application can appear in every deadlock and never be the victim.
    ///
    /// Kept as its own report rather than charts bolted onto the Deadlocks grid, but both run off
    /// <c>dbo.DeadlockScope</c>, so the same instance, date range and filters mean the same thing on both.
    /// </summary>
    internal class DeadlockChartsReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ProcedureName = "DeadlockCharts_Get",
            QualifiedProcedureName = "dbo.DeadlockCharts_Get",
            ReportVisibilityRole = "public",
            ReportName = "Deadlock Charts",
            Description = "Deadlocks over time, and their distribution by signature, application, database, login, host and procedure.",
            TriggerCollectionTypes = new List<string> { "Deadlocks" },
            ChartVisible = true,
            TableVisible = false,
            // Seven charts: the time series reads best full width, the six pies compare best in a grid.
            ChartLayoutColumns = 3,
            Charts = new List<CustomReportChart>
            {
                new()
                {
                    TableIndex = 0,
                    Title = "Deadlocks Over Time",
                    // The axis is the period the user picked, not the period the deadlocks happened to span.
                    // Deadlocks are sparse by nature: without this, two of them ten minutes apart are drawn
                    // across an axis ten minutes wide whether the range selected was an hour or a month, which
                    // says nothing about how often they happen and puts the later one hard against the right
                    // hand edge as though it had just occurred.
                    BindXAxisToDateRange = true,
                    // Points rather than a line: deadlocks are discrete events counted per bucket, and a
                    // line between two buckets implies values in between that were never measured.
                    Config = new ChartConfiguration
                    {
                        ChartType = ChartTypes.Scatter,
                        ChartTitle = "Over Time",
                        XColumn = "EventTime",
                        MetricColumns = new[] { "Deadlocks" },
                        YAxisLabel = "Count",
                        YAxisFormat = "N0",
                        // Red squares rather than the palette's first colour and shape.  The palette is for
                        // telling several series apart; this chart has one series, and what it counts is a
                        // fault, so it is drawn the colour the rest of the app draws failures.
                        SeriesStyles = new Dictionary<string, ChartSeriesStyle>
                        {
                            ["Deadlocks"] = ChartSeriesStyle.FromColor(DashColors.Fail, ChartMarkers.Square)
                        }
                    },
                    // Clicking a point opens the deadlocks it counted.  EventTimeEnd is the exclusive end of
                    // the bucket, which the proc returns because only it knows how wide the buckets are.
                    DrillDown = DrillDown(new Dictionary<string, string>
                    {
                        { "@FromDate", "EventTime" },
                        { "@ToDate", "EventTimeEnd" }
                    })
                },
                Pie(1, "Signature", "@Signature"),
                Pie(2, "Application", "@ApplicationName"),
                Pie(3, "Database", "@DatabaseName"),
                Pie(4, "Login", "@LoginName"),
                Pie(5, "Host", "@HostName"),
                Pie(6, "Procedure", "@ProcedureName")
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Over Time",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["EventTime"] = new ColumnMetadata { Alias = "Event Time" },
                        ["Deadlocks"] = new ColumnMetadata(),
                        ["Victims"] = new ColumnMetadata(),
                        // The end of the bucket EventTime starts.  Carried for the drill-down rather than
                        // to be read, so it stays out of the grid.
                        ["EventTimeEnd"] = new ColumnMetadata { Visible = false },
                        // Shown when the collection is disabled; the proc returns this instead.
                        ["Message"] = new ColumnMetadata()
                    }
                },
                [1] = Result("Signature"),
                [2] = Result("Application"),
                [3] = Result("Database"),
                [4] = Result("Login"),
                [5] = Result("Host"),
                [6] = Result("Procedure")
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@InstanceID", ParamType = "INT" },
                    new() { ParamName = "@FromDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@ToDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@Signature", ParamType = "VARCHAR" },
                    new() { ParamName = "@DateGroupingMin", ParamType = "INT" },
                    new() { ParamName = "@TopN", ParamType = "INT" },
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
                    ParameterName = "@VictimsOnly",
                    Name = "Processes",
                    PickerItems = new Dictionary<object, string>
                    {
                        [false] = "All involved",
                        [true] = "Victims only"
                    },
                    DefaultValue = false,
                    MenuBar = true,
                    DataType = typeof(bool)
                },
                new()
                {
                    ParameterName = "@TopN",
                    Name = "Slices",
                    PickerItems = new Dictionary<object, string>
                    {
                        [5] = "Top 5",
                        [10] = "Top 10",
                        [15] = "Top 15",
                        [25] = "Top 25"
                    },
                    DefaultValue = 10,
                    MenuBar = true,
                    DataType = typeof(int)
                }
            }
        };

        /// <summary>
        /// Every pie result set has the same GroupValue / Deadlocks shape, so they differ only by which
        /// result they read, what the chart is called, and which filter a slice drills into.
        /// </summary>
        private static CustomReportChart Pie(int tableIndex, string title, string parameterName) => new()
        {
            TableIndex = tableIndex,
            Title = title,
            Config = new PieChartConfiguration
            {
                ChartTitle = title,
                CategoryColumn = "GroupValue",
                ValueColumn = "Deadlocks",
                // The proc has already rolled the tail into an "Other" row, so the chart must not roll a
                // second time - that would produce two slices both called Other.
                MinSlicePercent = 0,
                InnerRadius = 0.5,
                DataLabelMode = PieLabelMode.None
            },
            // Clicking a slice opens the deadlocks it counted, filtered to that value - the same drill-down
            // the grid report's grouped result offers, from the picture rather than from the table.
            DrillDown = DrillDown(new Dictionary<string, string> { { parameterName, "GroupValue" } }),
            // Neither slice is a value anything can be filtered on: "Other" is the tail the proc rolled up,
            // and "(none)" is where the value was null.
            DrillDownExcludedValues = new List<string> { "Other", "(none)" }
        };

        /// <summary>
        /// A drill-down into the deadlocks report.  In the same window, so the charts and the detail read as
        /// one report with a back button rather than as a trail of windows; ctrl-click still opens a new one.
        ///
        /// The two reports share their filter parameters, so the charts' own filters are carried across and
        /// the detail shows what the clicked slice counted rather than a superset of it.  Victims Only is
        /// the one that matters most: it changes what a slice means, from the applications a deadlock
        /// involved to the ones that were rolled back.
        /// </summary>
        private static DrillDownLinkColumnInfo DrillDown(Dictionary<string, string> columnToParameterMap) => new()
        {
            DrillDownMode = DrillDownMode.ExistingWindow,
            ReportProcedureName = "DeadlockSummary_Get",
            ColumnToParameterMap = columnToParameterMap,
            CarryUserParameters = true
        };

        private static CustomReportResult Result(string name) => new()
        {
            ResultName = name,
            Columns = new Dictionary<string, ColumnMetadata>
            {
                ["GroupValue"] = new ColumnMetadata { Alias = name },
                ["Deadlocks"] = new ColumnMetadata()
            }
        };
    }
}