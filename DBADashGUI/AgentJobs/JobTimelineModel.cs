using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using static DBADashGUI.AgentJobs.TimelineRow;

namespace DBADashGUI.AgentJobs
{
    /// <summary>How the top level rows of the timeline are ordered.</summary>
    internal enum TimelineSort
    {
        Name,
        FirstStart,
        TotalDuration,
        Failures
    }

    /// <summary>One execution (or, with date grouping, a group of executions) of a job or job step.</summary>
    internal sealed class TimelineBar
    {
        public Guid JobId { get; init; }
        public string JobName { get; init; }
        public int StepId { get; init; }
        public string StepName { get; init; }

        /// <summary>Start and end as recorded, in the app time zone - not clipped to the selected period.</summary>
        public DateTime Start { get; init; }

        public DateTime End { get; set; }
        public int DurationSec { get; init; }
        public RunStatus Status { get; init; }
        public int Executions { get; init; }

        /// <summary>
        /// Runs by status, indexed by <see cref="RunStatus"/>, for a bar that merges runs of different statuses - its
        /// <see cref="Status"/> is the worst of them.  Null when every run has the bar's status.
        /// </summary>
        public int[] StatusRuns { get; init; }

        /// <summary>How many of the bar's runs had <paramref name="status"/>.</summary>
        public int RunsWith(RunStatus status) => StatusRuns != null ? StatusRuns[(int)status] : status == Status ? Executions : 0;

        /// <summary>
        /// msdb instance_id of the history row, which identifies the execution.  Null while still running, and for a date
        /// group of more than one execution.
        /// </summary>
        public int? HistoryId { get; init; }

        /// <summary>Index of the source row in the DataTable the model was built from.</summary>
        public int SourceIndex { get; init; }

        /// <summary>The instance the job is on.</summary>
        public int InstanceId { get; init; }

        /// <summary>
        /// A span of an instance row on the multi-instance timeline: <see cref="Executions"/> overlapping (or nearly)
        /// executions of any of its jobs, with the worst of their statuses.  <see cref="JobName"/> is the instance name.
        /// </summary>
        public bool IsSummary { get; init; }

        /// <summary>A single execution (finished or running) that execution details can be shown for.</summary>
        public bool IsSingleExecution => Executions == 1 && JobId != Guid.Empty && (HistoryId.HasValue || IsRunning);

        /// <summary>Which of its row's stacked lanes the bar is drawn in, so overlapping runs don't hide each other.</summary>
        public int Lane { get; set; }

        public TimelineRowModel Row { get; set; }

        public bool IsRunning => Status == RunStatus.InProgress;

        public string StepLabel => StepId == 0 ? "Job outcome" : $"Step {StepId}: {StepName}";

        /// <summary>
        /// The colour for a status, for the current theme.  The dark theme's chart background (Gray8) is close in
        /// lightness to the dark success and fail colours (about 1.2:1), so a bar a few pixels wide all but vanished.
        /// The dark theme uses lighter shades instead - 2.7:1 or better against the background, with white text still
        /// readable on them.
        /// </summary>
        public static Color StatusColor(RunStatus status) => DBADashUser.IsDarkTheme ? DarkStatusColor(status) : LightStatusColor(status);

        private static Color DarkStatusColor(RunStatus status) => status switch
        {
            RunStatus.Succeeded => DashColors.GreenLight,
            RunStatus.Retry => DashColors.Warning,
            RunStatus.InProgress => DashColors.ProgressBarTo,
            RunStatus.Cancelled => DashColors.Gray3,
            _ => DashColors.RedLight
        };

        private static Color LightStatusColor(RunStatus status) => status switch
        {
            RunStatus.Succeeded => DashColors.Success,
            RunStatus.Retry => DashColors.Warning,
            RunStatus.InProgress => DashColors.BlueLight,
            RunStatus.Cancelled => DashColors.Gray5,
            _ => DashColors.Fail
        };

        public static string StatusText(RunStatus status) => status switch
        {
            RunStatus.InProgress => "Running",
            _ => status.ToString()
        };
    }


    internal enum TimelineRowKind
    {
        Job,
        Step,

        /// <summary>An instance on the multi-instance timeline.  Its bars are summary spans; its jobs load when expanded.</summary>
        Instance
    }

    /// <summary>
    /// A row of the timeline: a job, or with steps included, a job (its outcome) with a child row per step.  On the
    /// multi-instance timeline the top level rows are instances, with their jobs beneath.
    /// </summary>
    internal sealed class TimelineRowModel
    {
        public TimelineRowKind Kind { get; init; } = TimelineRowKind.Job;
        public string Label { get; init; }
        public Guid JobId { get; init; }
        public string JobName { get; init; }
        public int InstanceId { get; init; }

        public bool IsGroup => Children.Count > 0 || CanLoadChildren;
        public List<TimelineRowModel> Children { get; } = new();
        public TimelineRowModel Parent { get; set; }
        public List<TimelineBar> Bars { get; } = new();
        public bool Collapsed { get; set; }

        /// <summary>The children haven't been fetched yet - they're loaded the first time the row is expanded.</summary>
        public bool CanLoadChildren { get; set; }

        /// <summary>The children are being fetched.</summary>
        public bool Loading { get; set; }

        /// <summary>Why loading the children failed, shown on the row.</summary>
        public string LoadError { get; set; }

        /// <summary>Executions by status, for an instance row whose own bars are summary spans.</summary>
        public Dictionary<RunStatus, int> StatusCounts { get; set; }

        /// <summary>Lanes needed for the bars currently shown.  Set by <see cref="PackLanes"/>.</summary>
        public int LaneCount { get; private set; } = 1;

        // Layout, in content pixels.  Set by the chart.
        public float Top { get; set; }

        public float Height { get; set; }
        public int Depth => Parent == null ? 0 : Parent.Depth + 1;

        /// <summary>The longest bar in the row, which bounds how far before a time a bar overlapping it can start.</summary>
        private long _maxBarTicks;

        /// <summary>The status filter the lanes were last packed for, so they are only packed again when it changes.</summary>
        private int? _packedFor;

        /// <summary>The row and every row beneath it.</summary>
        public IEnumerable<TimelineBar> AllBars => Bars.Concat(Children.SelectMany(c => c.AllBars));

        /// <summary>
        /// Order the bars by start, which <see cref="BarsBetween"/> relies on.  Call after adding bars.
        /// </summary>
        public void BarsChanged()
        {
            Bars.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.End.CompareTo(b.End));
            _maxBarTicks = Bars.Count == 0 ? 0 : Bars.Max(b => (b.End - b.Start).Ticks);
            _packedFor = null;
        }

        /// <summary>
        /// The bars that overlap <paramref name="from"/> to <paramref name="to"/>, found by binary search rather than by
        /// looking at every bar - a busy row on a long period can have thousands.
        /// </summary>
        public IEnumerable<TimelineBar> BarsBetween(DateTime from, DateTime to)
        {
            // A bar that overlaps "from" starts no earlier than the longest bar's length before it
            var earliest = from.Ticks - _maxBarTicks < DateTime.MinValue.Ticks ? DateTime.MinValue : from.AddTicks(-_maxBarTicks);
            int lo = 0, hi = Bars.Count;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (Bars[mid].Start < earliest) lo = mid + 1;
                else hi = mid;
            }
            for (var i = lo; i < Bars.Count && Bars[i].Start <= to; i++)
            {
                if (Bars[i].End >= from) yield return Bars[i];
            }
        }

        /// <summary>
        /// Stack overlapping bars into lanes: each bar goes in the first lane that is free by the time it starts.
        /// Only bars that pass <paramref name="visible"/> are placed, so hiding a status closes the gaps it leaves.
        /// <paramref name="filterKey"/> identifies the filter, so the work is skipped when nothing has changed.
        /// </summary>
        public void PackLanes(Func<TimelineBar, bool> visible, int filterKey)
        {
            if (_packedFor == filterKey) return;
            var laneEnds = new List<DateTime>();
            foreach (var bar in Bars)
            {
                if (!visible(bar)) continue;
                var lane = laneEnds.FindIndex(end => end <= bar.Start);
                if (lane < 0)
                {
                    lane = laneEnds.Count;
                    laneEnds.Add(bar.End);
                }
                else
                {
                    laneEnds[lane] = bar.End;
                }
                bar.Lane = lane;
            }
            LaneCount = Math.Max(1, laneEnds.Count);
            _packedFor = filterKey;
        }
    }

    /// <summary>
    /// The rows and bars of the job timeline, built from dbo.JobTimeline_Get - or, across instances, from
    /// dbo.JobTimelineSummary_Get with each instance's jobs added when it's expanded.
    /// </summary>
    internal sealed class JobTimelineModel
    {
        public List<TimelineRowModel> Roots { get; } = new();
        public List<TimelineBar> Bars { get; } = new();

        public static JobTimelineModel Empty { get; } = new();

        /// <summary>Each row is a step of one job, so name order means step order.</summary>
        public bool StepRows { get; private set; }

        /// <summary>The top level rows are instances.</summary>
        public bool MultiInstance { get; private set; }

        /// <param name="dt">Timeline data with dates already converted to the app time zone</param>
        /// <param name="singleJob">One job is shown, so each step is a row of its own rather than a child of the job</param>
        /// <param name="includeSteps">Steps are included, so each job gets a child row per step</param>
        /// <param name="instanceId">The instance the data is for</param>
        public static JobTimelineModel Build(DataTable dt, bool singleJob, bool includeSteps, int instanceId = 0)
        {
            var model = new JobTimelineModel();
            var hasJobId = dt.Columns.Contains("job_id");
            var hasHistoryId = dt.Columns.Contains("instance_id");
            var hasStatusRuns = dt.Columns.Contains("FailedRuns");
            var index = 0;
            var jobs = new Dictionary<string, TimelineRowModel>();
            var steps = new Dictionary<(string, int), TimelineRowModel>();

            foreach (DataRow r in dt.Rows)
            {
                var bar = new TimelineBar
                {
                    InstanceId = instanceId,
                    JobId = hasJobId && r["job_id"] != DBNull.Value ? (Guid)r["job_id"] : Guid.Empty,
                    JobName = (string)r["name"],
                    StepId = (int)r["step_id"],
                    StepName = (string)r["step_name"],
                    Start = (DateTime)r["RunDateTime"],
                    End = (DateTime)r["FinishDateTime"],
                    DurationSec = (int)r["RunDurationSec"],
                    Status = (RunStatus)r["run_status"],
                    Executions = (int)r["Executions"],
                    HistoryId = hasHistoryId && r["instance_id"] != DBNull.Value ? (int)r["instance_id"] : null,
                    StatusRuns = hasStatusRuns && (int)r["Executions"] > 1 ? StatusRunsOf(r) : null,
                    SourceIndex = index++
                };
                if (bar.End <= bar.Start) bar.End = bar.Start.AddSeconds(1);
                model.Bars.Add(bar);

                TimelineRowModel row;
                if (singleJob)
                {
                    row = GetOrAdd(steps, (bar.JobName, bar.StepId), () => new TimelineRowModel
                    {
                        Kind = TimelineRowKind.Step,
                        Label = bar.StepId == 0 ? "Outcome" : $"{bar.StepId} : {bar.StepName}",
                        JobId = bar.JobId,
                        JobName = bar.JobName,
                        InstanceId = instanceId
                    }, model.Roots);
                }
                else
                {
                    var job = GetOrAdd(jobs, bar.JobName, () => new TimelineRowModel { Label = bar.JobName, JobId = bar.JobId, JobName = bar.JobName, InstanceId = instanceId }, model.Roots);
                    if (includeSteps && bar.StepId != 0)
                    {
                        row = GetOrAdd(steps, (bar.JobName, bar.StepId), () => new TimelineRowModel
                        {
                            Kind = TimelineRowKind.Step,
                            Label = $"{bar.StepId} : {bar.StepName}",
                            JobId = bar.JobId,
                            JobName = bar.JobName,
                            InstanceId = instanceId,
                            Parent = job
                        }, job.Children);
                    }
                    else
                    {
                        row = job;
                    }
                }
                bar.Row = row;
                row.Bars.Add(bar);
            }

            foreach (var root in model.Roots)
            {
                root.BarsChanged();
                foreach (var child in root.Children) child.BarsChanged();
            }
            model.StepRows = singleJob;
            return model;
        }

        /// <summary>
        /// The multi-instance timeline from dbo.JobTimelineSummary_Get: a collapsed row per instance, showing its
        /// executions merged into spans.  Its jobs are added with <see cref="SetChildren"/> when it's expanded.
        /// </summary>
        /// <param name="ds">Instances, spans and status counts, with dates already converted to the app time zone</param>
        public static JobTimelineModel BuildSummary(DataSet ds)
        {
            var model = new JobTimelineModel { MultiInstance = true };
            var instances = new Dictionary<int, TimelineRowModel>();
            foreach (DataRow r in ds.Tables[0].Rows)
            {
                var id = (int)r["InstanceID"];
                var row = new TimelineRowModel
                {
                    Kind = TimelineRowKind.Instance,
                    Label = (string)r["InstanceDisplayName"],
                    JobName = string.Empty,
                    InstanceId = id,
                    CanLoadChildren = true,
                    Collapsed = true,
                    StatusCounts = new Dictionary<RunStatus, int>()
                };
                instances.Add(id, row);
                model.Roots.Add(row);
            }
            foreach (DataRow r in ds.Tables[1].Rows)
            {
                if (!instances.TryGetValue((int)r["InstanceID"], out var row)) continue;
                var bar = new TimelineBar
                {
                    IsSummary = true,
                    InstanceId = row.InstanceId,
                    JobName = row.Label,
                    StepName = string.Empty,
                    Start = (DateTime)r["RunDateTime"],
                    End = (DateTime)r["FinishDateTime"],
                    DurationSec = (int)r["RunDurationSec"],
                    Status = (RunStatus)r["run_status"],
                    Executions = (int)r["Executions"],
                    StatusRuns = ds.Tables[1].Columns.Contains("FailedRuns") && (int)r["Executions"] > 1 ? StatusRunsOf(r) : null,
                    Row = row
                };
                if (bar.End <= bar.Start) bar.End = bar.Start.AddSeconds(1);
                row.Bars.Add(bar);
                model.Bars.Add(bar);
            }
            foreach (DataRow r in ds.Tables[2].Rows)
            {
                if (!instances.TryGetValue((int)r["InstanceID"], out var row)) continue;
                row.StatusCounts[(RunStatus)(int)r["run_status"]] = (int)r["Executions"];
            }
            foreach (var root in model.Roots) root.BarsChanged();
            return model;
        }

        /// <summary>Put the jobs of an expanded instance beneath it.</summary>
        public void SetChildren(TimelineRowModel parent, JobTimelineModel children)
        {
            Bars.RemoveAll(b => b.Row?.Parent == parent);
            parent.Children.Clear();
            foreach (var child in children.Roots)
            {
                child.Parent = parent;
                parent.Children.Add(child);
            }
            Bars.AddRange(children.Bars);
            parent.CanLoadChildren = false;
            parent.Loading = false;
            parent.LoadError = null;
        }

        /// <summary>The run counts by status of a merged row, indexed by <see cref="RunStatus"/>.</summary>
        internal static readonly (RunStatus Status, string Column)[] StatusRunColumns =
        {
            (RunStatus.Failed, "FailedRuns"),
            (RunStatus.Succeeded, "SucceededRuns"),
            (RunStatus.Retry, "RetryRuns"),
            (RunStatus.Cancelled, "CancelledRuns"),
            (RunStatus.InProgress, "RunningRuns")
        };

        private static int[] StatusRunsOf(DataRow r)
        {
            var runs = new int[5];
            foreach (var (status, column) in StatusRunColumns)
            {
                if (r[column] != DBNull.Value) runs[(int)status] = Convert.ToInt32(r[column]);
            }
            return runs;
        }

        /// <summary>
        /// The model to count the legend from, when this one was put together from data at different levels of detail
        /// (the full period with a zoomed in window in place) - the full period load alone counts every run once.
        /// </summary>
        public JobTimelineModel CountsFrom { get; set; }

        private static TimelineRowModel GetOrAdd<TKey>(Dictionary<TKey, TimelineRowModel> lookup, TKey key, Func<TimelineRowModel> create, List<TimelineRowModel> addTo)
        {
            if (lookup.TryGetValue(key, out var row)) return row;
            row = create();
            lookup.Add(key, row);
            addTo.Add(row);
            return row;
        }

        private static bool Matches(TimelineRowModel row, string text) =>
            row.Label.Contains(text, StringComparison.OrdinalIgnoreCase) || row.JobName.Contains(text, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// An instance always counts as a match: the multi-instance summary is already filtered by job name on the server,
        /// so an instance is only there if it has jobs that match - even ones with no runs in the period, which leave a
        /// gap rather than a missing instance once its jobs are loaded.
        /// </summary>
        private static bool MatchesOrHasMatch(TimelineRowModel row, string text) =>
            row.Kind == TimelineRowKind.Instance || Matches(row, text) || row.Children.Any(c => MatchesOrHasMatch(c, text));

        /// <summary>Top level rows that match <paramref name="filter"/> themselves, or have a row beneath them that does.</summary>
        public IEnumerable<TimelineRowModel> MatchingRoots(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return Roots;
            var text = filter.Trim();
            return Roots.Where(r => MatchesOrHasMatch(r, text));
        }

        /// <summary>
        /// An instance counts its <see cref="TimelineRowModel.StatusCounts"/>: exact, and already filtered by job name on
        /// the server.  Counting its jobs' bars instead would change the totals just by expanding it - and when zoomed in,
        /// some of those bars carry a share of a merged bar's runs, estimated by time.
        /// </summary>
        public static bool CountsWholeInstance(TimelineRowModel root) => root.Kind == TimelineRowKind.Instance && root.StatusCounts != null;

        /// <summary>Executions by status for the legend, of the top level rows matching <paramref name="filter"/>.</summary>
        public Dictionary<RunStatus, int> LegendCounts(string filter)
        {
            if (CountsFrom != null) return CountsFrom.LegendCounts(filter);
            var counts = new Dictionary<RunStatus, int>();
            foreach (var root in MatchingRoots(filter))
            {
                if (CountsWholeInstance(root))
                {
                    foreach (var (status, count) in root.StatusCounts) counts[status] = counts.GetValueOrDefault(status) + count;
                    continue;
                }
                // A job's own bars are its outcomes, so steps aren't counted on top - unless outcomes aren't included
                foreach (var bar in OwnBars(root))
                {
                    foreach (var (status, _) in StatusRunColumns)
                    {
                        var runs = bar.RunsWith(status);
                        if (runs > 0) counts[status] = counts.GetValueOrDefault(status) + runs;
                    }
                }
            }
            return counts;
        }

        /// <summary>
        /// The rows to draw, top to bottom: filtered by name and visible status, sorted, and with collapsed groups'
        /// children left out.  A row that matches the filter shows all of its children; a row shown only because
        /// something beneath it matches shows only the matching children.  Each row's lanes are packed for the bars
        /// that are shown.
        /// </summary>
        public List<TimelineRowModel> VisibleRows(string filter, Func<TimelineBar, bool> visible, int visibleKey, TimelineSort sort, Func<RunStatus, bool> statusShown)
        {
            var text = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();
            var rows = new List<TimelineRowModel>();
            AddRows(rows, Roots, text, visible, visibleKey, sort, statusShown);
            return rows;
        }

        private void AddRows(List<TimelineRowModel> rows, IEnumerable<TimelineRowModel> siblings, string text, Func<TimelineBar, bool> visible, int visibleKey, TimelineSort sort, Func<RunStatus, bool> statusShown)
        {
            var level = siblings;
            if (text != null) level = level.Where(r => MatchesOrHasMatch(r, text));
            // An unloaded instance stays: what's hidden is decided by its summary spans
            if (statusShown != null) level = level.Where(r => HasShownRuns(r, visible, statusShown));
            foreach (var row in Sort(level.ToList(), sort))
            {
                row.PackLanes(visible, visibleKey);
                rows.Add(row);
                if (row.Collapsed || row.Children.Count == 0) continue;
                // A matching job shows all its steps, but an instance whose name matches doesn't show all its jobs - the filter is
                // on job names, and the instance is there because of the jobs that match
                AddRows(rows, row.Children, text == null || (row.Kind != TimelineRowKind.Instance && Matches(row, text)) ? null : text, visible, visibleKey, sort, statusShown);
            }
        }

        /// <summary>
        /// Whether a row has runs with a status that's shown, so it stays when other statuses are hidden.  An instance
        /// also goes by its own counts - the ones the legend shows - so it isn't hidden while the legend says it has
        /// matching runs, even if none of its spans can show them (a server without the per-status span counts, where a
        /// span is judged by its worst status).  Otherwise there'd be no row to expand to reach the jobs.
        /// </summary>
        private static bool HasShownRuns(TimelineRowModel row, Func<TimelineBar, bool> visible, Func<RunStatus, bool> statusShown) =>
            row.AllBars.Any(visible)
            || (row.Kind == TimelineRowKind.Instance && row.StatusCounts != null && row.StatusCounts.Any(kv => kv.Value > 0 && statusShown(kv.Key)));

        private IEnumerable<TimelineRowModel> Sort(List<TimelineRowModel> rows, TimelineSort sort)
        {
            if (rows.Count == 0) return rows;
            if (rows[0].Kind == TimelineRowKind.Step)
            {
                // Steps are always in step order - except a single job's rows, which follow the chosen sort below
                if (!StepRows || sort == TimelineSort.Name) return rows.OrderBy(r => r.Bars.Count == 0 ? 0 : r.Bars[0].StepId);
            }
            var byName = StringComparer.OrdinalIgnoreCase;
            return sort switch
            {
                TimelineSort.FirstStart => rows.OrderBy(r => OwnBars(r).Select(b => b.Start).DefaultIfEmpty(DateTime.MaxValue).Min()).ThenBy(r => r.Label, byName),
                TimelineSort.TotalDuration => rows.OrderByDescending(r => OwnBars(r).Sum(b => b.IsSummary ? b.DurationSec : (long)b.DurationSec * b.Executions)).ThenBy(r => r.Label, byName),
                TimelineSort.Failures => rows.OrderByDescending(Failures).ThenBy(r => r.Label, byName),
                _ => rows.OrderBy(r => r.Label, byName)
            };
        }

        /// <summary>A row's own bars, or with none (a job whose outcomes aren't included), the bars beneath it.</summary>
        private static IEnumerable<TimelineBar> OwnBars(TimelineRowModel row) => row.Bars.Count > 0 ? row.Bars : row.AllBars;

        private static int Failures(TimelineRowModel row) => row.StatusCounts != null
            ? row.StatusCounts.Where(kv => kv.Key is RunStatus.Failed or RunStatus.Retry).Sum(kv => kv.Value)
            : OwnBars(row).Sum(b => b.RunsWith(RunStatus.Failed) + b.RunsWith(RunStatus.Retry));
    }
}
