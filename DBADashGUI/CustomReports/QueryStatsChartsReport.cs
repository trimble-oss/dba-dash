using DBADashGUI.Charts;
using LiveChartsCore.Measure;
using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// The chart view of the query stats data - what the instance spent its time on, over time and in
    /// proportion.
    ///
    /// Normalised by default, because the absolute figure per bucket is the one number a stacked chart
    /// cannot show honestly: the first and last buckets of any range are usually partial, and a range the
    /// service was not collecting for all of has buckets covering less time than their width, so both would
    /// draw short columns that mean nothing.  Dividing by the time each bucket actually covered fixes both,
    /// and the Units picker gives the absolute numbers back to anyone who wants them.
    ///
    /// Bands are statements, qualified by the object they belong to.  Object level is what Object Execution
    /// Stats already does, and does better, so the value here is the level below it: which statement inside
    /// the procedure.  The object pie is still here for the case that level hides - a procedure whose cost is
    /// spread across forty statements, none of them a band in the chart.  The per plan breakdown belongs in
    /// the grid report.
    ///
    /// Everything not charted individually ends up in a single {Other} band, whether it fell past the Series
    /// picker or was rolled up by the collection itself.  Those are genuinely different - stored detail
    /// against detail that never existed - but as two slices named {Other} and {Other queries} the difference
    /// reads as a mistake rather than as information.
    ///
    /// The fourth chart is where that difference lives instead, and the one to look at before trusting the
    /// other three.  It splits the window's CPU into what is named, what was rolled up, and what could not be
    /// attributed at all, which is also what says which lever to pull: a large rolled up share means raising
    /// Query Stats Top N on the connection, not raising the Series picker here.
    ///
    /// Kept as its own report rather than charts bolted onto the grid, but both run off the same tables and
    /// share their instance, database and date range, so the same context means the same thing on both.
    /// </summary>
    internal class QueryStatsChartsReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ProcedureName = "QueryStatsCharts_Get",
            QualifiedProcedureName = "dbo.QueryStatsCharts_Get",
            ReportVisibilityRole = "public",
            ReportName = "Query Stats Charts",
            Description = "Statement level resource usage over time, and its distribution by object, statement and database, with the share of the window that is rolled up or unattributed.",
            TriggerCollectionTypes = new List<string> { DBADash.CollectionType.QueryStats.ToString() },
            SwitchTo = new ReportSwitch
            {
                ProcedureName = "QueryStats_Get",
                Text = "Show Data",
                ToolTipText = "Switch to the grid of queries behind these charts",
                Image = Properties.Resources.Table_16x
            },
            ChartVisible = true,
            TableVisible = false,
            // Five charts: four columns leaves exactly the spare cells the layout gives to the first chart, so
            // the time series is full width with the four pies in a row beneath it.
            ChartLayoutColumns = 4,
            Charts = new List<CustomReportChart>
            {
                new()
                {
                    TableIndex = 0,
                    Title = "Over Time",
                    // The axis is the period asked for, not the period the data happens to span, so a range
                    // with a quiet hour in it shows that hour as quiet rather than compressing it away.
                    BindXAxisToDateRange = true,
                    Config = new ChartConfiguration
                    {
                        ChartType = ChartTypes.StackedColumn,
                        ChartTitle = "Over Time",
                        XColumn = "TimeBucket",
                        // Long format: one row per bucket per series, stacked by the series column, so the
                        // proc does not have to pivot into a column per query.
                        MetricColumn = "Value",
                        SeriesColumn = "Series",
                        // Bands are statements, so a legend would be ten lines of query text under a chart it
                        // is meant to explain.  The tooltip names the band under the pointer - and on a
                        // stacked column it reports that band alone rather than the whole column, so the
                        // reader gets one statement and its value.
                        LegendPosition = LegendPosition.Hidden,
                        // Deliberately unlabelled: the measure and the units are both pickers, so any fixed
                        // label here would be wrong four times out of five.
                        YAxisFormat = "N1"
                    },
                    // Clicking a band opens the grid report for that statement in exactly the window the column
                    // covered.  The {Other} band carries no statement, so clicking it opens the window's
                    // queries whole - which is what the band stands for.
                    DrillDown = new DrillDownLinkColumnInfo
                    {
                        DrillDownMode = DrillDownMode.ExistingWindow,
                        ReportProcedureName = "QueryStats_Get",
                        ColumnToParameterMap = new Dictionary<string, string>
                        {
                            { "@FromDate", "TimeBucket" },
                            { "@ToDate", "TimeBucketEnd" },
                            { "@StatementID", "StatementID" },
                            { "@InstanceID", "InstanceID" }
                        },
                        CarryUserParameters = true,
                        ChildPanelTitle = "Queries"
                    }
                },
                new()
                {
                    TableIndex = 1,
                    Title = "By Statement",
                    Config = PieConfig("By Statement", hideLegend: true),
                    // A statement drills into its own plans - passing @StatementID is what the grid report
                    // reads as the plan grain - which is where a statement that changed plan shows as two rows.
                    DrillDown = new DrillDownLinkColumnInfo
                    {
                        DrillDownMode = DrillDownMode.ExistingWindow,
                        ReportProcedureName = "QueryStats_Get",
                        ColumnToParameterMap = new Dictionary<string, string>
                        {
                            { "@StatementID", "StatementID" },
                            // The instance the slice belongs to, so a drill-down across an estate lands on
                            // that one rather than on every instance running a statement that reads the same.
                            { "@InstanceID", "InstanceID" }
                        },
                        CarryUserParameters = true,
                        ChildPanelTitle = "Plans"
                    },
                    // {Other} is several statements at once, so there is no id to filter the grid by.
                    DrillDownExcludedValues = new List<string> { "{Other}" }
                },
                new()
                {
                    TableIndex = 2,
                    Title = "By Object",
                    Config = PieConfig("By Object", hideLegend: true),
                    // A slice is an object or a statement label, both of which the grid report can filter on.
                    // The rollup slices are not values anything can be filtered by, so they are excluded.
                    DrillDown = new DrillDownLinkColumnInfo
                    {
                        DrillDownMode = DrillDownMode.ExistingWindow,
                        ReportProcedureName = "QueryStats_Get",
                        ColumnToParameterMap = new Dictionary<string, string>
                        {
                            { "@ObjectName", "DrillValue" },
                            { "@InstanceID", "InstanceID" }
                        },
                        CarryUserParameters = true,
                        ChildPanelTitle = "Queries"
                    },
                    // {Other} stands for several things at once and {Ad hoc} for statements with no object,
                    // so neither is a value the grid could be filtered by.
                    DrillDownExcludedValues = new List<string> { "{Other}", "{Ad hoc}" }
                },
                new()
                {
                    TableIndex = 3,
                    Title = "By Database",
                    Config = PieConfig("By Database"),
                    DrillDown = new DrillDownLinkColumnInfo
                    {
                        DrillDownMode = DrillDownMode.ExistingWindow,
                        ReportProcedureName = "QueryStats_Get",
                        ColumnToParameterMap = new Dictionary<string, string>
                        {
                            { "@DatabaseID", "DatabaseID" },
                            { "@InstanceID", "InstanceID" }
                        },
                        CarryUserParameters = true,
                        ChildPanelTitle = "Queries"
                    },
                    // {Other} is several databases at once, so there is no id to filter the grid by.
                    DrillDownExcludedValues = new List<string> { "{Other}" }
                },
                new()
                {
                    TableIndex = 4,
                    Title = "CPU Coverage",
                    Config = new PieChartConfiguration
                    {
                        ChartTitle = "CPU Coverage",
                        CategoryColumn = "Series",
                        ValueColumn = "Value",
                        // Three slices at most, and each one matters however small: a two percent
                        // unattributed slice is still the answer to "why don't these numbers add up".
                        MinSlicePercent = 0,
                        InnerRadius = 0.5,
                        DataLabelMode = PieLabelMode.None
                    }
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Over Time",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["TimeBucket"] = new ColumnMetadata { Alias = "Time" },
                        ["Series"] = new ColumnMetadata(),
                        ["Value"] = new ColumnMetadata { FormatString = "N1" },
                        // Carried for the drill-down rather than to be read.
                        ["TimeBucketEnd"] = new ColumnMetadata { Visible = false },
                        ["StatementID"] = new ColumnMetadata { Visible = false },
                        ["InstanceID"] = new ColumnMetadata { Visible = false },
                        // Shown when the collection is disabled; the proc returns this instead.
                        ["Message"] = new ColumnMetadata(),
                        ["Url"] = new ColumnMetadata
                        {
                            Link = new UrlLinkColumnInfo { TargetColumn = "Url" }
                        }
                    }
                },
                [1] = new CustomReportResult
                {
                    ResultName = "Statement",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["Series"] = new ColumnMetadata { Alias = "Statement" },
                        ["Value"] = new ColumnMetadata { FormatString = "N1" },
                        // The label a slice can draw is cut to fit a chart; the grid has the whole statement.
                        ["StatementText"] = new ColumnMetadata
                        {
                            Alias = "Full Statement",
                            Link = new TextLinkColumnInfo
                            {
                                TargetColumn = "StatementText",
                                TextHandling = SchemaCompare.CodeEditor.CodeEditorModes.SQL
                            },
                            Description = "The statement in full, as the slice's label cannot be - click to view.  An ad hoc query is its template: the literal values and comments its variants differ in are taken out, because the slice is all of them together.  Blank for {Other}, which is several statements, and for a statement whose text has not been collected yet."
                        },
                        ["StatementID"] = new ColumnMetadata { Visible = false },
                        ["InstanceID"] = new ColumnMetadata { Visible = false }
                    }
                },
                [2] = Result("Object"),
                [3] = Result("Database"),
                [4] = Result("Coverage")
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
                    new() { ParamName = "@DateGroupingMin", ParamType = "INT" },
                    new() { ParamName = "@TopN", ParamType = "INT" },
                    new() { ParamName = "@Measure", ParamType = "VARCHAR" },
                    new() { ParamName = "@Units", ParamType = "VARCHAR" },
                    new() { ParamName = "@ExcludeRollups", ParamType = "BIT" },
                    // The floor below which a series or database joins {Other}.  Editable from the Parameters
                    // dialog rather than given a picker: it is a tuning value, not a question anyone asks of
                    // the data.  A slice with no width has no hover area, and a pie series with no hover area
                    // answers the hit test everywhere, which is how one invisible database ends up named in
                    // every other slice's tooltip.
                    new() { ParamName = "@MinSlicePercent", ParamType = "DECIMAL" }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@Measure",
                    Name = "Measure",
                    PickerItems = new Dictionary<object, string>
                    {
                        ["CPU"] = "CPU",
                        ["Duration"] = "Duration",
                        ["Executions"] = "Executions",
                        ["Reads"] = "Logical reads",
                        ["Writes"] = "Logical writes"
                    },
                    DefaultValue = "CPU",
                    MenuBar = true,
                    DataType = typeof(string)
                },
                new()
                {
                    ParameterName = "@Units",
                    Name = "Units",
                    PickerItems = new Dictionary<object, string>
                    {
                        ["PerSecond"] = "Per second",
                        ["Total"] = "Total"
                    },
                    DefaultValue = "PerSecond",
                    MenuBar = true,
                    DataType = typeof(string)
                },
                new()
                {
                    ParameterName = "@ExcludeRollups",
                    Name = "Rollups",
                    // Named for what it shows rather than as a yes/no, because the two answers are different
                    // pictures: with the rollups the height is the work the instance did, without them it is
                    // the named queries only.
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
                    ParameterName = "@TopN",
                    Name = "Series",
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
        /// The two distribution pies share a Series / Value shape, so they differ only by which result they
        /// read and what they are called.
        /// </summary>
        private static CustomReportChart Pie(int tableIndex, string title) => new()
        {
            TableIndex = tableIndex,
            Title = title,
            Config = PieConfig(title)
        };

        /// <param name="hideLegend">
        /// For the pies whose slices are named by query text.  A legend of ten statement labels is taller than
        /// the pie it explains, and in a row of four pies that leaves a chart the size of a postage stamp with
        /// a wall of text under it.  The slices are ordered largest first and the tooltip names the one under
        /// the pointer, which is how a reader uses these anyway - the pie is for "is one thing dominating",
        /// and the name of that thing is one hover away.  The database and coverage pies keep their legends:
        /// their labels are short enough to fit.
        /// </param>
        private static PieChartConfiguration PieConfig(string title, bool hideLegend = false) => new()
        {
            ChartTitle = title,
            CategoryColumn = "Series",
            ValueColumn = "Value",
            LegendPosition = hideLegend ? LegendPosition.Hidden : LegendPosition.Bottom,
            // The proc has already capped the slices and rolled the tail into one, so the chart must not roll
            // a second time - that would produce two slices both called Other.
            MinSlicePercent = 0,
            InnerRadius = 0.5,
            DataLabelMode = PieLabelMode.None
        };

        private static CustomReportResult Result(string name) => new()
        {
            ResultName = name,
            Columns = new Dictionary<string, ColumnMetadata>
            {
                ["Series"] = new ColumnMetadata { Alias = name },
                // Window totals rather than rates, whatever the Units picker says: a pie shows a share, and
                // the share of a total is the same number either way.  Only the time series is normalised,
                // where it is bucket to bucket comparability that is at stake.
                ["Value"] = new ColumnMetadata { FormatString = "N1" },
                ["DrillValue"] = new ColumnMetadata { Visible = false },
                ["InstanceID"] = new ColumnMetadata { Visible = false },
                ["DatabaseID"] = new ColumnMetadata { Visible = false },
                ["StatementID"] = new ColumnMetadata { Visible = false }
            }
        };
    }
}
