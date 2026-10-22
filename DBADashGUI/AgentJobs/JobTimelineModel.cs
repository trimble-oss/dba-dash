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

        /// <summary>Which of its row's stacked lanes the bar is drawn in, so overlapping runs don't hide each other.</summary>
        public int Lane { get; set; }

        public TimelineRowModel Row { get; set; }

        public bool IsRunning => Status == RunStatus.InProgress;

        public string StepLabel => StepId == 0 ? "Job outcome" : $"Step {StepId}: {StepName}";

        public static Color StatusColor(RunStatus status) => status switch
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

    /// <summary>
    /// A row of the timeline: a job, or with steps included, a job (its outcome) with a child row per step.
    /// </summary>
    internal sealed class TimelineRowModel
    {
        public string Label { get; init; }
        public Guid JobId { get; init; }
        public string JobName { get; init; }
        public bool IsGroup => Children.Count > 0;
        public List<TimelineRowModel> Children { get; } = new();
        public TimelineRowModel Parent { get; set; }
        public List<TimelineBar> Bars { get; } = new();
        public bool Collapsed { get; set; }

        /// <summary>Lanes needed for the bars currently shown.  Set by <see cref="PackLanes"/>.</summary>
        public int LaneCount { get; private set; } = 1;

        // Layout, in content pixels.  Set by the chart.
        public float Top { get; set; }

        public float Height { get; set; }
        public int Depth => Parent == null ? 0 : 1;

        /// <summary>The row and every row beneath it.</summary>
        public IEnumerable<TimelineBar> AllBars => Bars.Concat(Children.SelectMany(c => c.AllBars));

        /// <summary>
        /// Stack overlapping bars into lanes: each bar goes in the first lane that is free by the time it starts.
        /// Only bars that pass <paramref name="visible"/> are placed, so hiding a status closes the gaps it leaves.
        /// </summary>
        public void PackLanes(Func<TimelineBar, bool> visible)
        {
            var laneEnds = new List<DateTime>();
            foreach (var bar in Bars.Where(visible).OrderBy(b => b.Start).ThenBy(b => b.End))
            {
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
        }
    }

    /// <summary>
    /// The rows and bars of the job timeline, built from dbo.JobTimeline_Get.
    /// </summary>
    internal sealed class JobTimelineModel
    {
        public List<TimelineRowModel> Roots { get; } = new();
        public List<TimelineBar> Bars { get; } = new();

        public static JobTimelineModel Empty { get; } = new();

        /// <summary>Each row is a step of one job, so name order means step order.</summary>
        public bool StepRows { get; private set; }

        /// <param name="dt">Timeline data with dates already converted to the app time zone</param>
        /// <param name="singleJob">One job is shown, so each step is a row of its own rather than a child of the job</param>
        /// <param name="includeSteps">Steps are included, so each job gets a child row per step</param>
        public static JobTimelineModel Build(DataTable dt, bool singleJob, bool includeSteps)
        {
            var model = new JobTimelineModel();
            var hasJobId = dt.Columns.Contains("job_id");
            var jobs = new Dictionary<string, TimelineRowModel>();
            var steps = new Dictionary<(string, int), TimelineRowModel>();

            foreach (DataRow r in dt.Rows)
            {
                var bar = new TimelineBar
                {
                    JobId = hasJobId && r["job_id"] != DBNull.Value ? (Guid)r["job_id"] : Guid.Empty,
                    JobName = (string)r["name"],
                    StepId = (int)r["step_id"],
                    StepName = (string)r["step_name"],
                    Start = (DateTime)r["RunDateTime"],
                    End = (DateTime)r["FinishDateTime"],
                    DurationSec = (int)r["RunDurationSec"],
                    Status = (RunStatus)r["run_status"],
                    Executions = (int)r["Executions"]
                };
                if (bar.End <= bar.Start) bar.End = bar.Start.AddSeconds(1);
                model.Bars.Add(bar);

                TimelineRowModel row;
                if (singleJob)
                {
                    row = GetOrAdd(steps, (bar.JobName, bar.StepId), () => new TimelineRowModel
                    {
                        Label = bar.StepId == 0 ? "Outcome" : $"{bar.StepId} : {bar.StepName}",
                        JobId = bar.JobId,
                        JobName = bar.JobName
                    }, model.Roots);
                }
                else
                {
                    var job = GetOrAdd(jobs, bar.JobName, () => new TimelineRowModel { Label = bar.JobName, JobId = bar.JobId, JobName = bar.JobName }, model.Roots);
                    if (includeSteps && bar.StepId != 0)
                    {
                        row = GetOrAdd(steps, (bar.JobName, bar.StepId), () => new TimelineRowModel
                        {
                            Label = $"{bar.StepId} : {bar.StepName}",
                            JobId = bar.JobId,
                            JobName = bar.JobName,
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
                root.Children.Sort((a, b) => a.Bars[0].StepId.CompareTo(b.Bars[0].StepId));
            }
            model.StepRows = singleJob;
            return model;
        }

        private static TimelineRowModel GetOrAdd<TKey>(Dictionary<TKey, TimelineRowModel> lookup, TKey key, Func<TimelineRowModel> create, List<TimelineRowModel> addTo)
        {
            if (lookup.TryGetValue(key, out var row)) return row;
            row = create();
            lookup.Add(key, row);
            addTo.Add(row);
            return row;
        }

        /// <summary>
        /// The rows to draw, top to bottom: filtered by job name and visible status, sorted, and with collapsed
        /// groups' children left out.  Each row's lanes are packed for the bars that are shown.
        /// </summary>
        public List<TimelineRowModel> VisibleRows(string filter, Func<TimelineBar, bool> visible, TimelineSort sort, bool hideEmpty)
        {
            IEnumerable<TimelineRowModel> roots = Roots;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                roots = roots.Where(r => r.JobName.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase) || r.Label.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase));
            }
            if (hideEmpty)
            {
                roots = roots.Where(r => r.AllBars.Any(visible));
            }
            roots = sort switch
            {
                TimelineSort.FirstStart => roots.OrderBy(r => r.AllBars.Min(b => b.Start)).ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase),
                TimelineSort.TotalDuration => roots.OrderByDescending(r => r.AllBars.Where(b => b.StepId == 0 || !r.IsGroup).Sum(b => (long)b.DurationSec * b.Executions)).ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase),
                TimelineSort.Failures => roots.OrderByDescending(r => r.AllBars.Count(b => b.Status is RunStatus.Failed or RunStatus.Retry)).ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase),
                _ when StepRows => roots.OrderBy(r => r.Bars[0].StepId),
                _ => roots.OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
            };

            var rows = new List<TimelineRowModel>();
            foreach (var root in roots)
            {
                root.PackLanes(visible);
                rows.Add(root);
                if (root.Collapsed) continue;
                foreach (var child in root.Children.Where(c => !hideEmpty || c.Bars.Any(visible)))
                {
                    child.PackLanes(visible);
                    rows.Add(child);
                }
            }
            return rows;
        }
    }
}
