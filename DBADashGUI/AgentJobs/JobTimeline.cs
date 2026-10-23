using DBADashGUI.Performance;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.AgentJobs.TimelineRow;

namespace DBADashGUI.AgentJobs
{
    public partial class JobTimeline : UserControl, ISetContext, IRefreshData
    {
        public const string Template = @"
<html>
    <head>
        <style>
            .tt { padding: 0px 0px 0px 0px; }
            .tt h1 {color: ##SUCCESS_F_COLOR##; background-color:##SUCCESS_B_COLOR##; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;font-size: 15px }
            .ttf { padding: 0px 0px 0px 0px; }
            .ttf h1 {color: ##FAIL_F_COLOR##;; background-color:##FAIL_B_COLOR##; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;font-size: 15px }
            .ttw { padding: 0px 0px 0px 0px;}
            .ttw h1 {color: ##WARNING_F_COLOR##; background-color:##WARNING_B_COLOR##; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;font-size: 15px }
            h1 {color: ##TITLE_F_COLOR##; background-color:##TITLE_B_COLOR##; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;}
            body {background-color: ##BODY_B_COLOR##; color: ##BODY_F_COLOR## }
            div.google-visualization-tooltip { background-color: ##TOOLTIP_B_COLOR##; color: ##TOOLTIP_F_COLOR##; font-size: 15px;font-family: Arial, Helvetica, sans-serif;  }
        </style>
        <script type=""text/javascript"" src=""https://www.gstatic.com/charts/loader.js""></script>
        <script type=""text/javascript"">
            google.charts.load('current', {
                'packages': ['timeline']
            });
            google.charts.setOnLoadCallback(drawChart);

            function drawChart() {
                var container = document.getElementById('job-timeline');
                var chart = new google.visualization.Timeline(container);
                var dataTable = new google.visualization.DataTable();

                dataTable.addColumn({
                    type: 'string',
                    id: 'Job'
                });
                dataTable.addColumn({
                    type: 'string',
                    id: 'label'
                });
                dataTable.addColumn({
                    type: 'string',
                    role: 'style'
                });
                dataTable.addColumn({
                    type: 'string',
                    role: 'tooltip'
                });
                dataTable.addColumn({
                    type: 'date',
                    id: 'Start'
                });
                dataTable.addColumn({
                    type: 'date',
                    id: 'End'
                });
                dataTable.addRows([
                    ##DATA##
                ]);

                var options = {
                    timeline: {
                        colorByRowLabel: true,
                        rowLabelStyle: {
                            color: '##LABEL_COLOR##'
                        }
                    },
                    backgroundColor: '##CHART_B_COLOR##',
                    alternatingRowStyle: false,
                    height: ##HEIGHT##,
                    hAxis: {
                        format: '##DATEFORMAT##'
                    },
                    tooltip: { isHtml: true },
                };

                google.visualization.events.addListener(chart, 'ready', function() {
                    // find <rect> elements for outer rectangle formatting
                    var rects = container.getElementsByTagName('rect');
                    Array.prototype.forEach.call(rects, function(rect) {
                        if (rect.getAttribute('stroke') === '#9a9a9a') {
                            rect.setAttribute('stroke', '##GRID_COLOR##');
                            rect.setAttribute('stroke-width', '');
                            rect.setAttribute('stroke-dasharray', '1,1');
                        }
                    });

                    // find <path> elements for vertical/horizontal gridlines
                    var paths = container.getElementsByTagName('path');
                    Array.prototype.forEach.call(paths, function(path) {
                        path.setAttribute('stroke', '##GRID_COLOR##');
                        path.setAttribute('stroke-dasharray', '1,1');
                        path.setAttribute('stroke-width', '0.5');
                    });

                    // find <text> elements for formatting axis labels
                    var labels = container.getElementsByTagName('text');
                    Array.prototype.forEach.call(labels, function(label) {
                        label.setAttribute('fill', '##LABEL_COLOR##');
                    });
                });

                chart.draw(dataTable, options);
            }
        </script>
    </head>
    <body>
        <h1>##SERVERNAME##</h1>
        <div id=""job-timeline""></div>
  </body>
</html>";

        public const string NoDataHTMLTemplate = @"
<html>
    <head>
        <style>
            h1 {color: ##TITLE_F_COLOR##; background-color:##TITLE_B_COLOR##; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;}
            h3 {font-family: Arial, Helvetica, sans-serif;}
            body {background-color: ##BODY_B_COLOR##; color: ##BODY_F_COLOR## }
        </style>
    </head>
    <body>
        <h1>##SERVERNAME##</h1>
        <h3>No Data</h3>
    </body>
</html>";

        private string html;
        private DataTable dt;
        private DateTime from;
        private DateTime to;
        private DBADashContext context;
        private string categoryKey;
        private string selectedCategory;
        private int mins = -1;

        /// <summary>Incremented by each load, so a slower earlier one can tell it has been overtaken.</summary>
        private int loadGeneration;

        /// <summary>
        /// The Auto date grouping: the level of detail follows the zoom.  Runs closer than about a pixel are merged, and
        /// zooming in fetches the time in view again with a smaller gap, down to each individual run.
        /// </summary>
        private const int AutoDateGrouping = -1;

        /// <summary>More bars than this and the fetch is repeated with a wider merge gap.</summary>
        private const int MaxBars = 50_000;

        private bool AutoDetail => DateGrouping == AutoDateGrouping;

        // The full period, at the level of detail that suits it
        private DataTable baseData;
        private DataSet baseSummary;
        private int baseGap;
        private bool baseCapped;
        private readonly Dictionary<int, DataTable> childBase = new();

        // Zoomed in: the time in view (and as much again either side) at a finer level of detail
        private DateTime? detailFromUtc;
        private DateTime? detailToUtc;
        private DataTable detailData;
        private DataSet detailSummary;
        private int detailGap;
        private int detailRequestedGap;
        private bool detailCapped;
        private int detailGeneration;

        /// <summary>Waits for zooming and panning to settle before fetching detail for the view.</summary>
        private readonly System.Windows.Forms.Timer viewTimer = new() { Interval = 350 };

        /// <summary>Limits how many instances' jobs are fetched at once on the multi-instance timeline.</summary>
        private readonly SemaphoreSlim childLoads = new(4);

        /// <summary>Waits for typing to pause before filtering on the server (multi-instance timeline).</summary>
        private readonly System.Windows.Forms.Timer filterTimer = new() { Interval = 600 };

        /// <summary>
        /// No single instance in context (the Agent Jobs node at the root or a folder): a summary row per instance, with
        /// each instance's jobs loaded when it's expanded.
        /// </summary>
        private bool MultiInstance => context.JobID == Guid.Empty && context.InstanceID <= 0 && context.InstanceIDs.Count != 1;

        private int InstanceId => context.InstanceID > 0 ? context.InstanceID : context.InstanceIDs.FirstOrDefault();

        /// <summary>Start the chart afresh on the next draw (new context or date range) rather than keeping the user's place.</summary>
        private bool resetView;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsActive { get; set; }

        private bool IncludeSteps => stepsToolStripMenuItem.Checked || bothToolStripMenuItem.Checked || context.JobID != Guid.Empty;
        private bool IncludeOutcome => outcomeToolStripMenuItem.Checked || bothToolStripMenuItem.Checked || context.JobID != Guid.Empty;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UseGlobalTime
        {
            get => !dateRangeToolStripMenuItem1.Visible;
            set => dateRangeToolStripMenuItem1.Visible = !value;
        }

        private DateTime FromDateUtc => UseGlobalTime ? DateRange.FromUTC : dateRangeToolStripMenuItem1.DateFromUtc;
        private DateTime ToDateUtc => UseGlobalTime ? DateRange.ToUTC : dateRangeToolStripMenuItem1.DateToUtc;

        public TimeSpan SelectedTimeSpan =>
            UseGlobalTime ? DateRange.TimeSpan : dateRangeToolStripMenuItem1.SelectedTimeSpan ?? DateRange.TimeSpan;

        private int DateGrouping
        {
            get => (int)tsDateGroup.Tag!;
            set
            {
                tsDateGroup.Tag = value;
                tsDateGroup.Text = value == AutoDateGrouping ? "Auto" : DateHelper.DateGroupString(value);
            }
        }

        public JobTimeline()
        {
            InitializeComponent();
            DateHelper.AddDateGroups(tsDateGroup, TsDateGroup_Click);
            var auto = new ToolStripMenuItem("Auto")
            {
                Tag = AutoDateGrouping,
                ToolTipText = "Combine runs too close together to tell apart at the current zoom, and show more detail as you zoom in"
            };
            auto.Click += TsDateGroup_Click;
            tsDateGroup.DropDownItems.Insert(0, auto);
            tsDateGroup.DropDownItems.Insert(1, new ToolStripSeparator());
            DateGrouping = AutoDateGrouping;
            AddSortOptions();
            AddRunFilterItems();
            chart.ChildrenRequested += Chart_ChildrenRequested;
            chart.ViewChanged += (_, _) =>
            {
                viewTimer.Stop();
                viewTimer.Start();
            };
            viewTimer.Tick += (_, _) =>
            {
                viewTimer.Stop();
                FetchDetailForView();
                FetchConcurrencyForView(false);
            };
            Disposed += (_, _) => viewTimer.Dispose();
            filterTimer.Tick += (_, _) =>
            {
                filterTimer.Stop();
                // Across instances the summary is filtered on the server; for one instance only the band is
                if (MultiInstance) RefreshData();
                else FetchConcurrencyForView(true);
            };
            Disposed += (_, _) => filterTimer.Dispose();
        }

        private void AddSortOptions()
        {
            (TimelineSort Sort, string Text)[] options =
            {
                (TimelineSort.Name, "Name"),
                (TimelineSort.FirstStart, "First start"),
                (TimelineSort.TotalDuration, "Total duration"),
                (TimelineSort.Failures, "Failures")
            };
            foreach (var (sort, text) in options)
            {
                var item = new ToolStripMenuItem(text) { Tag = sort, Checked = sort == chart.Sort };
                item.Click += (_, _) =>
                {
                    tsSort.CheckSingleItem(item);
                    tsSort.Text = text;
                    chart.Sort = sort;
                };
                tsSort.DropDownItems.Add(item);
            }
        }

        /// <summary>
        /// Filter by job name.  Across instances the jobs aren't loaded until an instance is expanded, so the summary is
        /// also fetched again filtered on the server - once typing pauses.
        /// </summary>
        private void Filter_TextChanged(object sender, EventArgs e)
        {
            chart.Filter = tsFilter.Text;
            if (context == null || (!MultiInstance && !chart.ShowConcurrency)) return;
            filterTimer.Stop();
            filterTimer.Start();
        }

        /// <summary>
        /// Show the execution the bar belongs to.  A running bar has no history row yet, so it's found by its start time -
        /// which also finds it if the job has finished since the timeline was loaded.
        /// </summary>
        private void Chart_BarActivated(object sender, TimelineBar bar)
        {
            if (!bar.IsSingleExecution) return;
            try
            {
                var form = bar.IsRunning
                    ? new JobExecutionForm(bar.InstanceId, bar.JobId, bar.JobName, null, bar.Start.AppTimeZoneToUtc())
                    : new JobExecutionForm(bar.InstanceId, bar.JobId, bar.JobName, bar.HistoryId, null);
                form.ShowSingleInstance();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void Chart_JobActivated(object sender, TimelineBar bar)
        {
            if (bar.JobId == Guid.Empty) return;
            try
            {
                var jobContext = CommonData.GetDBADashContext(bar.InstanceId);
                jobContext.Type = SQLTreeItem.TreeType.AgentJob;
                jobContext.JobID = bar.JobId;
                jobContext.ObjectName = bar.JobName;
                var frm = new JobInfoForm { DBADashContext = jobContext };
                frm.ShowSingleInstance();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void TsDateGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = Convert.ToInt32(ts.Tag);
            RefreshData();
        }

        /// <summary>
        /// Get DataTable with Job Timeline data.  Dates converted to app timezone
        /// </summary>
        private static DataTable GetJobTimelineData(int InstanceID, DateTime from, DateTime to, RunFilters filters, Guid job_id, bool steps, bool outcome, int dateGroup, int mergeGapSec = 0)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.JobTimeline_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("@FromDate", from);
            cmd.Parameters.AddWithValue("@ToDate", to);
            cmd.Parameters.AddWithValue("@IncludeSteps", steps);
            cmd.Parameters.AddWithValue("@IncludeOutcome", outcome);
            cmd.Parameters.AddGuidIfNotEmpty("@job_id", job_id);
            cmd.Parameters.AddStringIfNotNullOrEmpty("category", filters.Category);
            cmd.Parameters.AddWithValue("@MinDurationSec", filters.MinDurationSec);
            cmd.Parameters.AddWithValue("@ProblemsOnly", filters.ProblemsOnly);
            cmd.Parameters.AddWithValue("@DateGroupingMin", dateGroup);
            cmd.Parameters.AddWithValue("@MergeGapSec", mergeGapSec);

            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        /// <summary>
        /// The multi-instance timeline: a summary row per instance (dbo.JobTimelineSummary_Get), dates converted to the
        /// app time zone.  Executions closer together than <paramref name="mergeGapSec"/> are merged into one span.
        /// </summary>
        private static DataSet GetSummaryData(IEnumerable<int> instanceIds, DateTime from, DateTime to, RunFilters filters, int mergeGapSec)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.JobTimelineSummary_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("@InstanceIDs", instanceIds.AsDataTable());
            cmd.Parameters.AddWithValue("@FromDate", from);
            cmd.Parameters.AddWithValue("@ToDate", to);
            cmd.Parameters.AddStringIfNotNullOrEmpty("@category", filters.Category);
            cmd.Parameters.AddStringIfNotNullOrEmpty("@JobName", filters.JobName);
            cmd.Parameters.AddWithValue("@MinDurationSec", filters.MinDurationSec);
            cmd.Parameters.AddWithValue("@ProblemsOnly", filters.ProblemsOnly);
            cmd.Parameters.AddWithValue("@MergeGapSec", mergeGapSec);
            var ds = new DataSet();
            da.Fill(ds);
            foreach (DataTable table in ds.Tables)
            {
                var t = table;
                DateHelper.ConvertUTCToAppTimeZone(ref t);
            }
            return ds;
        }

        /// <summary>
        /// The most jobs running at once in each <paramref name="sliceSec"/> slice of the period, for the concurrency band -
        /// filled in from dbo.JobTimelineConcurrency_Get, which only returns slices where a job starts or finishes.
        /// </summary>
        private static int[] GetConcurrencyData(IEnumerable<int> instanceIds, DateTime from, DateTime to, RunFilters filters, int sliceSec)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.JobTimelineConcurrency_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("@InstanceIDs", instanceIds.AsDataTable());
            cmd.Parameters.AddWithValue("@FromDate", from);
            cmd.Parameters.AddWithValue("@ToDate", to);
            cmd.Parameters.AddWithValue("@BucketSec", sliceSec);
            cmd.Parameters.AddStringIfNotNullOrEmpty("@category", filters.Category);
            cmd.Parameters.AddStringIfNotNullOrEmpty("@JobName", filters.JobName);
            cmd.Parameters.AddWithValue("@MinDurationSec", filters.MinDurationSec);
            cmd.Parameters.AddWithValue("@ProblemsOnly", filters.ProblemsOnly);
            var dt = new DataTable();
            da.Fill(dt);

            var peaks = new int[(int)Math.Ceiling((to - from).TotalSeconds / sliceSec)];
            var rows = dt.AsEnumerable().ToDictionary(r => (int)r["Bucket"], r => ((int)r["MaxRunning"], (int)r["EndRunning"]));
            var running = 0; // carried through slices where nothing starts or finishes
            for (var i = 0; i < peaks.Length; i++)
            {
                if (rows.TryGetValue(i, out var slice))
                {
                    peaks[i] = Math.Max(running, slice.Item1);
                    running = slice.Item2;
                }
                else
                {
                    peaks[i] = running;
                }
            }
            return peaks;
        }

        /// <summary>The filters on what's fetched, taken together when a fetch starts so it can't mix old and new.</summary>
        private sealed record RunFilters(string Category, string JobName, int MinDurationSec, bool ProblemsOnly);

        /// <summary>
        /// The job name filter only goes to the server across instances - for one instance every job is loaded and the
        /// chart filters them - but the concurrency band always counts just the jobs that match.
        /// </summary>
        private RunFilters CurrentFilters => new(selectedCategory, MultiInstance ? tsFilter.Text.Trim() : null, minDurationSec, problemsOnly);

        private int minDurationSec;
        private bool problemsOnly;

        // The concurrency band's data: the window and slice it was fetched for
        private DateTime? bandFromUtc;
        private DateTime? bandToUtc;
        private int bandSliceSec;
        private int bandGeneration;

        /// <summary>
        /// Fetch the concurrency band for the time in view (and as much again either side, like the detail), in slices of
        /// about a pixel - unless what's loaded already covers it at about the right size.
        /// </summary>
        private async void FetchConcurrencyForView(bool force)
        {
            if (context == null || !chart.ShowConcurrency || chart.ViewTo <= chart.ViewFrom || to <= from) return;
            var viewFrom = chart.ViewFrom.AppTimeZoneToUtc();
            var viewTo = chart.ViewTo.AppTimeZoneToUtc();
            var span = viewTo - viewFrom;
            var slice = Math.Max(1, (int)(span.TotalSeconds / Math.Max(200, chart.TimeAxisWidth)));
            if (!force && bandFromUtc <= viewFrom && bandToUtc >= viewTo && bandSliceSec <= slice * 2 && bandSliceSec * 2 >= slice) return;

            var windowFrom = viewFrom - span < from ? from : viewFrom - span;
            var windowTo = viewTo + span > to ? to : viewTo + span;
            var generation = loadGeneration;
            var band = ++bandGeneration;
            var instanceIds = MultiInstance ? context.RegularInstanceIDs.ToList() : new List<int> { InstanceId };
            var filters = CurrentFilters with { JobName = tsFilter.Text.Trim() };
            try
            {
                var peaks = await Task.Run(() => GetConcurrencyData(instanceIds, windowFrom, windowTo, filters, slice));
                if (generation != loadGeneration || band != bandGeneration) return;
                (bandFromUtc, bandToUtc, bandSliceSec) = (windowFrom, windowTo, slice);
                chart.SetConcurrency(windowFrom.ToAppTimeZone(), TimeSpan.FromSeconds(slice), peaks);
            }
            catch (Exception ex)
            {
                if (generation == loadGeneration && band == bandGeneration) chart.Banner = "Couldn't load how many jobs were running at once: " + ex.Message;
            }
        }

        private ToolStripDropDownButton tsRuns;
        private ToolStripMenuItem tsProblemsOnly;
        private ToolStripButton tsConcurrency;

        /// <summary>
        /// The run filters (minimum duration, problems only) and the concurrency band toggle.  The filters apply on the
        /// server, so they cut what's fetched as well as what's shown.
        /// </summary>
        private void AddRunFilterItems()
        {
            tsRuns = new ToolStripDropDownButton("All runs", Properties.Resources.Filter_16x) { ToolTipText = "Leave out short runs, or show only the runs with a problem" };
            (int Seconds, string Text)[] durations =
            {
                (0, "Any duration"), (10, "10 seconds or longer"), (60, "1 minute or longer"), (300, "5 minutes or longer"),
                (900, "15 minutes or longer"), (3600, "1 hour or longer")
            };
            foreach (var (seconds, text) in durations)
            {
                var item = new ToolStripMenuItem(text) { Tag = seconds, Checked = seconds == 0 };
                item.Click += (_, _) =>
                {
                    foreach (var other in tsRuns.DropDownItems.OfType<ToolStripMenuItem>().Where(i => i.Tag is int)) other.Checked = other == item;
                    minDurationSec = seconds;
                    RunFiltersChanged();
                };
                tsRuns.DropDownItems.Add(item);
            }
            tsRuns.DropDownItems.Add(new ToolStripSeparator());
            tsProblemsOnly = new ToolStripMenuItem("Problems only (failed, retry, cancelled)") { CheckOnClick = true };
            tsProblemsOnly.CheckedChanged += (_, _) =>
            {
                problemsOnly = tsProblemsOnly.Checked;
                RunFiltersChanged();
            };
            tsRuns.DropDownItems.Add(tsProblemsOnly);

            tsConcurrency = new ToolStripButton("Running at once", Properties.Resources.StackedAreaChart)
            {
                CheckOnClick = true,
                Checked = true,
                ToolTipText = "Show how many jobs were running at once, across every instance in view"
            };
            tsConcurrency.CheckedChanged += (_, _) =>
            {
                chart.ShowConcurrency = tsConcurrency.Checked && context?.JobID == Guid.Empty;
                FetchConcurrencyForView(true);
            };

            var at = toolStrip1.Items.IndexOf(tsSort) + 1;
            toolStrip1.Items.Insert(at, tsRuns);
            toolStrip1.Items.Insert(at + 1, tsConcurrency);
        }

        private void RunFiltersChanged()
        {
            var parts = new List<string>();
            if (minDurationSec > 0) parts.Add("≥ " + JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(minDurationSec)));
            if (problemsOnly) parts.Add("problems");
            tsRuns.Text = parts.Count == 0 ? "All runs" : string.Join(", ", parts);
            tsRuns.Font = new Font(tsRuns.Font, parts.Count == 0 ? FontStyle.Regular : FontStyle.Bold);
            RefreshData();
        }

        private static DataTable GetJobCategories(int instanceId, IEnumerable<int> instanceIds)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.JobCategories_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            if (instanceIds == null)
            {
                cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            }
            else
            {
                cmd.Parameters.AddWithValue("@InstanceIDs", instanceIds.AsDataTable());
            }
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        /// <summary>
        /// Refresh job categories menu if context has changed
        /// </summary>
        private void RefreshCategories()
        {
            var key = MultiInstance ? string.Join(",", context.RegularInstanceIDs.OrderBy(id => id)) : InstanceId.ToString();
            if (key != categoryKey)
            {
                string allCategoriesText = "ALL Categories";
                selectedCategory = string.Empty;
                tsCategories.Text = allCategoriesText;
                tsCategories.DropDownItems.Clear();
                var tsALL = new ToolStripMenuItem() { Text = allCategoriesText, Tag = string.Empty, Checked = true };
                tsALL.Click += Category_Selected;
                tsCategories.DropDownItems.Add(tsALL);
                tsCategories.DropDownItems.Add(new ToolStripSeparator());
                DataTable dtCategories = GetJobCategories(InstanceId, MultiInstance ? context.RegularInstanceIDs : null);
                foreach (DataRow row in dtCategories.Rows)
                {
                    string category = (string)row["category"];
                    var tsCat = new ToolStripMenuItem() { Text = category, Tag = category };
                    tsCat.Click += Category_Selected;
                    tsCategories.DropDownItems.Add(tsCat);
                }
                categoryKey = key;
            }
        }

        private void Category_Selected(object sender, EventArgs e)
        {
            string selected = (string)((ToolStripMenuItem)sender).Tag;
            string selectedText = ((ToolStripMenuItem)sender).Text;
            tsCategories.Text = selectedText;
            tsCategories.CheckSingleItem((ToolStripMenuItem)sender);
            selectedCategory = selected;
            RefreshData();
        }

        /// <summary>
        /// Load and draw the timeline.  The query runs off the UI thread - across many instances it can take a while - and
        /// a load that has been overtaken by a newer one (a refresh, a filter change, another node) is discarded.
        /// </summary>
        public async void RefreshData()
        {
            var generation = ++loadGeneration;
            try
            {
                if (mins != DateRange.DurationMins)
                {
                    // A fixed grouping is re-picked to suit a new period.  Auto adjusts to the zoom by itself.
                    if (!AutoDetail) DateGrouping = DateHelper.DateGrouping(Convert.ToInt32(SelectedTimeSpan.TotalMinutes), 1440);
                    mins = DateRange.DurationMins;
                }
                RefreshCategories();
                var queryFrom = FromDateUtc;
                var queryTo = ToDateUtc;
                var filters = CurrentFilters;
                var dateGroup = AutoDetail ? 0 : DateGrouping;
                var gap = AutoDetail ? GapFor(queryTo - queryFrom) : 0;
                tsRefresh.Enabled = false;

                if (MultiInstance)
                {
                    var instanceIds = context.RegularInstanceIDs.ToList();
                    var (ds, usedGap, capped) = await Task.Run(() => FetchCapped(g => GetSummaryData(instanceIds, queryFrom, queryTo, filters, g), d => d.Tables[1].Rows.Count, gap));
                    if (generation != loadGeneration) return;
                    SetBase(queryFrom, queryTo, usedGap, capped);
                    baseSummary = ds;
                    hTMLToolStripMenuItem.Enabled = false;
                }
                else
                {
                    var instanceId = InstanceId;
                    var jobId = context.JobID;
                    var steps = IncludeSteps;
                    var outcome = IncludeOutcome;
                    var (data, usedGap, capped) = await Task.Run(() => FetchCapped(g => GetJobTimelineData(instanceId, queryFrom, queryTo, filters, jobId, steps, outcome, dateGroup, g), d => d.Rows.Count, gap));
                    if (generation != loadGeneration) return;
                    SetBase(queryFrom, queryTo, usedGap, capped);
                    baseData = data;
                    hTMLToolStripMenuItem.Enabled = true;
                }
                Display();
                FetchConcurrencyForView(true);
                resetView = false;
            }
            catch (Exception ex)
            {
                if (generation == loadGeneration) CommonShared.ShowExceptionDialog(ex, "Error loading job timeline");
            }
            finally
            {
                if (generation == loadGeneration) tsRefresh.Enabled = true;
            }
        }

        /// <summary>Start again from a full period load: the zoomed in detail and each instance's jobs are dropped.</summary>
        private void SetBase(DateTime queryFrom, DateTime queryTo, int gap, bool capped)
        {
            from = queryFrom;
            to = queryTo;
            baseGap = gap;
            baseCapped = capped;
            baseData = null;
            baseSummary = null;
            ClearDetail();
            childBase.Clear();
        }

        private void ClearDetail()
        {
            // A detail fetch still running is for a view that's gone - don't let it land after this
            detailGeneration++;
            detailFromUtc = null;
            detailToUtc = null;
            detailData = null;
            detailSummary = null;
            detailCapped = false;
            detailGap = 0;
            detailRequestedGap = 0;
        }

        private bool DetailActive => detailFromUtc.HasValue;

        /// <summary>
        /// How close runs can be and still be merged: the time covered by the narrowest bar the chart draws (3 pixels), so
        /// runs are only merged where their bars would have run into each other anyway.  Less than a second means nothing
        /// is merged.
        /// </summary>
        private int GapFor(TimeSpan span) => (int)(span.TotalSeconds * MinBarPixels / Math.Max(200, chart.TimeAxisWidth));

        private const int MinBarPixels = 3;

        /// <summary>
        /// Fetch, and if that returns more than <see cref="MaxBars"/>, fetch once more with a gap wide enough to bring it
        /// under - a busy estate over a long period could otherwise be hundreds of thousands of bars.
        /// </summary>
        private static (T Data, int Gap, bool Capped) FetchCapped<T>(Func<int, T> fetch, Func<T, int> count, int gap)
        {
            var data = fetch(gap);
            var bars = count(data);
            if (bars <= MaxBars) return (data, gap, false);
            var wider = (int)Math.Max(Math.Max(gap, 1) * 2L, Math.Ceiling(Math.Max(gap, 1) * (double)bars / MaxBars * 1.5));
            return (fetch(wider), wider, true);
        }

        /// <summary>
        /// When the view settles after a zoom or pan, fetch more detail for it if what's loaded is too coarse - or doesn't
        /// cover it.  The detail is fetched for the view and as much again either side, so a small pan doesn't fetch again.
        /// Zoomed back out to where the full period load is fine enough, the detail is dropped.
        /// </summary>
        private async void FetchDetailForView()
        {
            if (context == null || !AutoDetail || chart.ViewTo <= chart.ViewFrom) return;
            var viewFrom = chart.ViewFrom.AppTimeZoneToUtc();
            var viewTo = chart.ViewTo.AppTimeZoneToUtc();
            var need = GapFor(viewTo - viewFrom);
            var zoomed = chart.ViewFrom > chart.PeriodFrom || chart.ViewTo < chart.PeriodTo;
            if (!zoomed || (need * 2 >= baseGap && !baseCapped))
            {
                if (!DetailActive) return;
                ClearDetail();
                Display();
                return;
            }
            // Compared with the gap asked for rather than the one used: a capped fetch is coarser than asked, and asking again would only be capped again
            if (DetailActive && detailFromUtc <= viewFrom && detailToUtc >= viewTo && detailRequestedGap <= need * 2) return;

            var span = viewTo - viewFrom;
            var windowFrom = viewFrom - span < from ? from : viewFrom - span;
            var windowTo = viewTo + span > to ? to : viewTo + span;
            var generation = loadGeneration;
            var detail = ++detailGeneration;
            var filters = CurrentFilters;
            try
            {
                if (MultiInstance)
                {
                    var instanceIds = context.RegularInstanceIDs.ToList();
                    var (ds, usedGap, capped) = await Task.Run(() => FetchCapped(g => GetSummaryData(instanceIds, windowFrom, windowTo, filters, g), d => d.Tables[1].Rows.Count, need));
                    if (generation != loadGeneration || detail != detailGeneration) return;
                    detailSummary = ds;
                    (detailGap, detailCapped) = (usedGap, capped);
                }
                else
                {
                    var instanceId = InstanceId;
                    var jobId = context.JobID;
                    var steps = IncludeSteps;
                    var outcome = IncludeOutcome;
                    var (data, usedGap, capped) = await Task.Run(() => FetchCapped(g => GetJobTimelineData(instanceId, windowFrom, windowTo, filters, jobId, steps, outcome, 0, g), d => d.Rows.Count, need));
                    if (generation != loadGeneration || detail != detailGeneration) return;
                    detailData = data;
                    (detailGap, detailCapped) = (usedGap, capped);
                }
                (detailFromUtc, detailToUtc, detailRequestedGap) = (windowFrom, windowTo, need);
                Display();
            }
            catch (Exception ex)
            {
                // The full period load is still shown, so just say why the detail is missing
                if (generation == loadGeneration && detail == detailGeneration) chart.Banner = "Couldn't load more detail: " + ex.Message;
            }
        }

        /// <summary>
        /// Draw what's loaded: the full period load, with the detail fetched for the zoomed in window in place of it there.
        /// Expanded instances are asked for their jobs again by the chart, for the same window.
        /// </summary>
        private void Display()
        {
            var windowFrom = detailFromUtc?.ToAppTimeZone();
            var windowTo = detailToUtc?.ToAppTimeZone();
            if (MultiInstance)
            {
                if (baseSummary == null) return;
                var ds = baseSummary;
                if (detailSummary != null)
                {
                    ds = baseSummary.Clone();
                    ds.Tables[0].Merge(detailSummary.Tables[0].Rows.Count >= baseSummary.Tables[0].Rows.Count ? detailSummary.Tables[0] : baseSummary.Tables[0]);
                    ds.Tables[1].Merge(Combine(baseSummary.Tables[1], detailSummary.Tables[1], windowFrom.Value, windowTo.Value));
                    ds.Tables[2].Merge(baseSummary.Tables[2]); // the legend counts the whole period
                }
                dt = null;
                html = null;
                chart.Title = $"{ds.Tables[0].Rows.Count:N0} instances";
                chart.SetData(JobTimelineModel.BuildSummary(ds), from.ToAppTimeZone(), to.ToAppTimeZone(), resetView);
            }
            else
            {
                if (baseData == null) return;
                dt = detailData == null ? baseData : Combine(baseData, detailData, windowFrom.Value, windowTo.Value);
                html = null;
                DrawTimeline();
            }
            chart.Banner = BannerText();
        }

        /// <summary>
        /// The full period rows outside the detail window, and the detail rows.  Inside the window the detail has the runs
        /// themselves.  A merged full period row that reaches into the window is trimmed back to the window's edges, so a
        /// job merged into one long bar still shows either side of it; a single run reaching in is left to the detail,
        /// which has it whole.
        /// </summary>
        private static DataTable Combine(DataTable full, DataTable detail, DateTime windowFrom, DateTime windowTo)
        {
            // The wider schema: a fetch with nothing to merge has no runs-by-status columns
            var combined = full.Columns.Count >= detail.Columns.Count ? full.Clone() : detail.Clone();
            foreach (DataRow r in full.Rows)
            {
                var start = (DateTime)r["RunDateTime"];
                var finish = (DateTime)r["FinishDateTime"];
                if (finish < windowFrom || start > windowTo)
                {
                    combined.ImportRow(r);
                    continue;
                }
                if (!r.Table.Columns.Contains("Executions") || (int)r["Executions"] <= 1) continue;
                if (start < windowFrom) AddTrimmed(combined, r, start, windowFrom);
                if (finish > windowTo) AddTrimmed(combined, r, windowTo, finish);
            }
            foreach (DataRow r in detail.Rows) combined.ImportRow(r);
            return combined;
        }

        private static void AddTrimmed(DataTable table, DataRow source, DateTime start, DateTime finish)
        {
            table.ImportRow(source);
            var row = table.Rows[^1];
            // The piece's share of the runs, by time - which runs fell where isn't known
            var whole = ((DateTime)source["FinishDateTime"] - (DateTime)source["RunDateTime"]).TotalSeconds;
            var share = whole <= 0 ? 1 : (finish - start).TotalSeconds / whole;
            var executions = 0;
            foreach (var (_, column) in JobTimelineModel.StatusRunColumns)
            {
                if (!table.Columns.Contains(column) || row[column] == DBNull.Value) continue;
                var runs = (int)Math.Round(Convert.ToInt32(row[column]) * share);
                row[column] = runs;
                executions += runs;
            }
            row["Executions"] = Math.Max(1, executions > 0 ? executions : (int)Math.Round((int)source["Executions"] * share));
            row["RunDateTime"] = start;
            row["FinishDateTime"] = finish;
            row["RunDurationSec"] = (int)Math.Max(1, (finish - start).TotalSeconds);
            if (table.Columns.Contains("instance_id")) row["instance_id"] = DBNull.Value;
        }

        /// <summary>Say when runs are being merged, so a merged bar isn't mistaken for one long run.</summary>
        private string BannerText()
        {
            if (!AutoDetail) return null;
            var gap = DetailActive ? detailGap : baseGap;
            var within = JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(gap));
            if (DetailActive ? detailCapped : baseCapped)
            {
                return $"Too many runs to show individually - runs within {within} of each other are combined.  Zoom in or filter for detail.";
            }
            // The instance spans are merged by design, so only job rows count
            // Zoomed in, only the detail for the window is at this zoom's level - the rest is off screen
            var shown = DetailActive ? detailData : baseData;
            var merged = MultiInstance ? childBase.Count > 0 && gap > 0 : gap > 0 && shown != null && shown.AsEnumerable().Any(r => (int)r["Executions"] > 1);
            return merged ? $"Runs within {within} of each other are combined at this zoom.  Zoom in to see each run." : null;
        }

        /// <summary>
        /// Fetch the jobs of an instance expanded on the multi-instance timeline: for the full period (kept, so collapsing
        /// and expanding again doesn't fetch it again) and, when zoomed in, the detail for the window.
        /// </summary>
        private async void Chart_ChildrenRequested(object sender, TimelineRowModel row)
        {
            var generation = loadGeneration;
            var queryFrom = from;
            var queryTo = to;
            var filters = CurrentFilters;
            var dateGroup = AutoDetail ? 0 : DateGrouping;
            var gap = baseGap;
            var (windowFrom, windowTo, windowGap) = (detailFromUtc, detailToUtc, detailGap);
            try
            {
                // Expand all can ask for hundreds - a few at a time is kinder to the repository
                await childLoads.WaitAsync();
                DataTable data;
                try
                {
                    if (generation != loadGeneration) return;
                    if (!childBase.TryGetValue(row.InstanceId, out var full))
                    {
                        full = await Task.Run(() => GetJobTimelineData(row.InstanceId, queryFrom, queryTo, filters, Guid.Empty, false, true, dateGroup, gap));
                        if (generation != loadGeneration) return;
                        childBase[row.InstanceId] = full;
                    }
                    data = full;
                    if (windowFrom.HasValue)
                    {
                        var detail = await Task.Run(() => GetJobTimelineData(row.InstanceId, windowFrom.Value, windowTo.Value, filters, Guid.Empty, false, true, 0, windowGap));
                        data = Combine(full, detail, windowFrom.Value.ToAppTimeZone(), windowTo.Value.ToAppTimeZone());
                    }
                }
                finally
                {
                    childLoads.Release();
                }
                if (generation != loadGeneration) return;
                chart.SetChildren(row, JobTimelineModel.Build(data, singleJob: false, includeSteps: false, row.InstanceId));
                chart.Banner = BannerText();
            }
            catch (Exception ex)
            {
                if (generation == loadGeneration) chart.SetChildrenError(row, ex.Message);
            }
        }

        /// <summary>
        /// Return a color based on job RunStatus
        /// </summary>
        private static Color GetStatusColor(RunStatus status, bool isTruncated)
        {
            if (isTruncated) /* Use a lighter colour to indicate job started before or finished after selected time period */
            {
                return status switch
                {
                    RunStatus.Succeeded => DashColors.GreenPale,
                    RunStatus.Retry => DashColors.YellowPale,
                    RunStatus.InProgress => DashColors.BluePale,
                    _ => DashColors.RedPale
                };
            }
            else
            {
                return status switch
                {
                    RunStatus.Succeeded => DashColors.Success,
                    RunStatus.Retry => DashColors.Warning,
                    RunStatus.InProgress => DashColors.TrimbleBlueDark,
                    _ => DashColors.Fail
                };
            }
        }

        private void DrawTimeline()
        {
            var singleJob = context.JobID != Guid.Empty;
            chart.Title = context.InstanceName;
            var model = JobTimelineModel.Build(dt, singleJob, IncludeSteps, InstanceId);
            // Zoomed in, part of what's drawn is shared out from merged bars by time - count the legend from the full
            // period load, where every run is counted once
            if (detailData != null && baseData != null && !ReferenceEquals(dt, baseData))
            {
                model.CountsFrom = JobTimelineModel.Build(baseData, singleJob, IncludeSteps, InstanceId);
            }
            chart.SetData(model, from.ToAppTimeZone(), to.ToAppTimeZone(), resetView);
            resetView = false;
        }

        /// <summary>
        /// Get HTML for Job timeline.  Only used for Copy HTML - the timeline itself is drawn by <see cref="JobTimelineChart"/>.
        /// </summary>
        private void GenerateHTML()
        {
            if (dt.Rows.Count == 0)
            {
                html = ReplaceColors(NoDataHTMLTemplate.Replace("##SERVERNAME##", context.InstanceName));
                return;
            }
            StringBuilder sb = new();
            DateTime appFrom = from.ToAppTimeZone();
            DateTime appTo = to.ToAppTimeZone();
            int rowCount = 0;
            TimelineRow previousTlr = new();
            List<RunStatus> statuses = new();
            foreach (DataRow r in dt.Rows)
            {
                TimelineRow tlr = new()
                {
                    ActualStart = (DateTime)r["RunDateTime"],
                    ActualEnd = (DateTime)r["FinishDateTime"],
                    Duration = (int)r["RunDurationSec"],
                    JobName = (string)r["name"],
                    Status = (RunStatus)r["run_status"],
                    Step = (string)r["step_name"],
                    ExecutionCount = (int)r["Executions"],
                    AppFrom = appFrom,
                    AppTo = appTo,
                    StepID = (int)r["step_id"]
                };
                if (context.JobID == Guid.Empty)
                {
                    tlr.Name = tlr.JobName + (IncludeSteps ? " | " + tlr.StepID : "");
                }
                else
                {
                    tlr.Name = tlr.StepID + " : " + tlr.Step;
                }

                if (tlr.Name != previousTlr.Name)
                {
                    rowCount++;
                    statuses.Clear();
                    statuses.Add(tlr.Status);
                }
                else if (tlr.Start < previousTlr.End && !statuses.Contains(tlr.Status))
                {
                    rowCount++;
                    statuses.Add(tlr.Status);
                }

                Color statusColor = GetStatusColor(tlr.Status, tlr.IsTruncated);
                // Dates are serialized with the "s" format, which drops fractional seconds. Compare the values the chart will see.
                var barStart = TruncateToSecond(tlr.Start);
                var barEnd = TruncateToSecond(tlr.End);
                if (barEnd <= barStart)
                {
                    barEnd = barStart.AddSeconds(1);
                }

                sb.AppendFormat("[ '{0}', '{1}', '{2}', '{3}', new Date('{4}'), new Date('{5}') ],\n",
                    tlr.EncodedName,
                    tlr.Status == RunStatus.InProgress ? "Running" : "",
                    statusColor.ToHexString(),
                    tlr.EncodedToolTip,
                    barStart.ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                    barEnd.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
                previousTlr = tlr;
            }
            html = ReplaceColors(Template.Replace("##DATEFORMAT##", DateFormat).Replace("##SERVERNAME##", context.InstanceName).Replace("##HEIGHT##", ChartHeight(rowCount).ToString()));
            html = html.Replace("##DATA##", sb.ToString());
        }

        public static string ReplaceColors(string html)
        {
            var theme = DBADashUser.SelectedTheme;
            return html.Replace("##WARNING_B_COLOR##", theme.WarningBackColor.ToHexString())
                .Replace("##WARNING_F_COLOR##", theme.WarningForeColor.ToHexString())
                .Replace("##FAIL_B_COLOR##", theme.CriticalBackColor.ToHexString())
                .Replace("##FAIL_F_COLOR##", theme.CriticalForeColor.ToHexString())
                .Replace("##SUCCESS_B_COLOR##", theme.SuccessBackColor.ToHexString())
                .Replace("##SUCCESS_F_COLOR##", theme.SuccessForeColor.ToHexString())
                .Replace("##TITLE_F_COLOR##", theme.TimelineTitleForeColor.ToHexString())
                .Replace("##TITLE_B_COLOR##", theme.TimelineTitleBackColor.ToHexString())
                .Replace("##BODY_B_COLOR##", theme.TimelineBodyBackColor.ToHexString())
                .Replace("##BODY_F_COLOR##", theme.TimelineBodyForeColor.ToHexString())
                .Replace("##LABEL_COLOR##", theme.TimelineLabelColor.ToHexString())
                .Replace("##CHART_B_COLOR##", theme.TimelineChartBackColor.ToHexString())
                .Replace("##GRID_COLOR##", theme.TimelineGridColor.ToHexString())
                .Replace("##TOOLTIP_B_COLOR##", theme.TimelineToolTipBackColor.ToHexString())
                .Replace("##TOOLTIP_F_COLOR##", theme.TimelineToolTipForeColor.ToHexString());
        }

        private static int ChartHeight(int rows)
        {
            return (42 * rows) + 100;
        }

        private static DateTime TruncateToSecond(DateTime value)
        {
            return new DateTime(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond), value.Kind);
        }

        /// <summary>
        /// Use a suitable date format based on duration.
        /// </summary>
        private string DateFormat => SelectedTimeSpan.TotalMinutes < 1500 ? "HH:mm" : "MMM dd HH:mm";

        public void SetContext(DBADashContext _context)
        {
            this.context = _context;
            IsActive = true;
            tsIncludeSteps.Visible = _context.JobID == Guid.Empty && !MultiInstance; // outcome only across instances
            // How many of one job were running at once isn't worth a band
            tsConcurrency.Visible = _context.JobID == Guid.Empty;
            chart.ShowConcurrency = tsConcurrency.Checked && _context.JobID == Guid.Empty;
            bandFromUtc = null;
            chart.SetConcurrency(default, TimeSpan.FromSeconds(1), null);
            // Opening the job from the job's own window would only close and reopen it
            chart.JobActivated -= Chart_JobActivated;
            if (FindForm() is not JobInfoForm) chart.JobActivated += Chart_JobActivated;
            resetView = true;
            RefreshData();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void Include_Steps(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            tsIncludeSteps.CheckSingleItem(item);
            stepsToolStripMenuItem.Checked = stepsToolStripMenuItem == sender;
            outcomeToolStripMenuItem.Checked = outcomeToolStripMenuItem == sender;
            tsIncludeSteps.Text = item.Text;
            RefreshData();
        }

        private void Copy_HTML(object sender, EventArgs e)
        {
            if (dt == null) return;
            if (html == null) GenerateHTML();
            Common.CopyHtmlToClipBoard(html);
        }

        private void Copy_Image(object sender, EventArgs e)
        {
            chart.CopyImage();
        }

        private void DateRangeChanged(object sender, EventArgs e)
        {
            resetView = true;
            RefreshData();
        }

        private void JobTimeline_Load(object sender, EventArgs e)
        {
            if (UseGlobalTime) return;
            if (DateRange.SelectedTimeSpan.HasValue)
            {
                dateRangeToolStripMenuItem1.SetTimeSpan(DateRange.SelectedTimeSpan.Value);
            }
            else
            {
                dateRangeToolStripMenuItem1.SetDateRangeUtc(DateRange.FromUTC, DateRange.ToUTC);
            }
        }
    }
}