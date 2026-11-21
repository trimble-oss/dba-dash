using DBADashGUI.AgentJobs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static DBADashGUI.AgentJobs.TimelineRow;

namespace DBADashGUI.ElasticJobs
{
    /// <summary>
    /// The rows and bars of the elastic job timeline, from dbo.ElasticJobTimeline_Get (unmerged, with steps):
    /// <list type="bullet">
    /// <item>A row per job, with a bar per job execution.</item>
    /// <item>Beneath it, a row per step with a bar per run of the step - from the first of its targets starting to the last finishing,
    /// coloured by the worst outcome.  A step runs against many databases at once, which as bars of their own stack into a lane each.</item>
    /// <item>Beneath each step (collapsed to start with), a row per target the step ran against, with a bar per execution.</item>
    /// </list>
    /// Executions of a step against its targets share the job execution's ID, which is how they're grouped into runs.
    /// </summary>
    internal static class ElasticJobTimelineModel
    {
        /// <summary>
        /// What the chart's bars don't carry.  For a bar of a target's execution, the <see cref="Target"/> and its <see cref="Lifecycle"/>
        /// (Skipped rather than the chart's Cancelled, say).  For a bar of a step's run, its targets by lifecycle, worst first.
        /// </summary>
        internal sealed record BarInfo(string Target, string Lifecycle, IReadOnlyList<(string Lifecycle, int Count)> TargetOutcomes);

        /// <summary>A step row's key, to keep it expanded or collapsed when the data is loaded again</summary>
        internal static string StepKey(TimelineRowModel row) => row.JobName + "|" + row.Label;

        /// <summary>True if the data has the target of each execution - not merged, so a row per target can be shown</summary>
        public static bool HasTargets(DataTable dt) => dt.Columns.Contains("target_database_name") && !dt.Columns.Contains("FailedRuns");

        /// <param name="dt">Timeline data with dates already converted to the app time zone.  The bars' HistoryId is the index of their row.</param>
        /// <param name="instanceId">The instance (job database) the data is for</param>
        /// <param name="stepCollapsed">Step rows by <see cref="StepKey"/> that the user expanded or collapsed.  Others start collapsed.</param>
        /// <param name="info">Gets the details of the bars for the tooltip</param>
        public static JobTimelineModel Build(DataTable dt, int instanceId, IReadOnlyDictionary<string, bool> stepCollapsed, Dictionary<TimelineBar, BarInfo> info)
        {
            var model = new JobTimelineModel();
            var jobs = new Dictionary<string, TimelineRowModel>();
            var steps = new Dictionary<(string Job, int Step), TimelineRowModel>();
            var targets = new Dictionary<(string Job, int Step, string Target), TimelineRowModel>();
            var runs = new Dictionary<(string Job, int Step, Guid Execution), List<TimelineBar>>();

            // A step against databases on more than one server needs the server in the database's label
            var multiServerSteps = dt.AsEnumerable()
                .Where(r => (int)r["step_id"] > 0 && r["target_server_name"] != DBNull.Value)
                .GroupBy(r => ((string)r["name"], (int)r["step_id"]))
                .Where(g => g.Select(r => (string)r["target_server_name"]).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            for (var i = 0; i < dt.Rows.Count; i++)
            {
                var r = dt.Rows[i];
                var jobName = (string)r["name"];
                var stepId = (int)r["step_id"];
                var executionId = r["job_execution_id"] == DBNull.Value ? (Guid?)null : (Guid)r["job_execution_id"];
                var bar = new TimelineBar
                {
                    InstanceId = instanceId,
                    JobId = (Guid)r["job_id"],
                    JobName = jobName,
                    StepId = stepId,
                    StepName = (string)r["step_name"],
                    Start = (DateTime)r["RunDateTime"],
                    End = (DateTime)r["FinishDateTime"],
                    DurationSec = (int)r["RunDurationSec"],
                    Status = (RunStatus)r["run_status"],
                    Executions = 1,
                    HistoryId = executionId.HasValue ? i : null,
                    SourceIndex = i
                };
                if (bar.End <= bar.Start) bar.End = bar.Start.AddSeconds(1);
                model.Bars.Add(bar);

                var job = GetOrAdd(jobs, jobName, () => new TimelineRowModel { Label = jobName, JobId = bar.JobId, JobName = jobName, InstanceId = instanceId }, model.Roots);
                if (stepId == 0)
                {
                    bar.Row = job;
                    job.Bars.Add(bar);
                    continue;
                }

                var step = GetOrAdd(steps, (jobName, stepId), () =>
                {
                    var row = new TimelineRowModel
                    {
                        Kind = TimelineRowKind.Step,
                        Label = $"{stepId} : {bar.StepName}",
                        JobId = bar.JobId,
                        JobName = jobName,
                        InstanceId = instanceId,
                        Parent = job
                    };
                    row.Collapsed = !stepCollapsed.TryGetValue(StepKey(row), out var collapsed) || collapsed;
                    return row;
                }, job.Children);

                var (label, description) = TargetName(r, multiServerSteps.Contains((jobName, stepId)));
                var target = GetOrAdd(targets, (jobName, stepId, label), () => new TimelineRowModel
                {
                    Kind = TimelineRowKind.Step,
                    Label = label,
                    JobId = bar.JobId,
                    JobName = jobName,
                    InstanceId = instanceId,
                    Parent = step
                }, step.Children);
                bar.Row = target;
                target.Bars.Add(bar);
                info[bar] = new BarInfo(description, r.Table.Columns.Contains("lifecycle") ? r["lifecycle"] as string : null, null);

                // Without an execution ID a target's execution is a run on its own
                var runKey = (jobName, stepId, executionId ?? Guid.NewGuid());
                if (!runs.TryGetValue(runKey, out var runBars))
                {
                    runBars = new List<TimelineBar>();
                    runs.Add(runKey, runBars);
                }
                runBars.Add(bar);
            }

            foreach (var ((jobName, stepId, _), runBars) in runs)
            {
                var step = steps[(jobName, stepId)];
                var first = runBars[0];
                var start = runBars.Min(b => b.Start);
                var end = runBars.Max(b => b.End);
                var outcomes = runBars
                    .GroupBy(b => (Lifecycle: info[b].Lifecycle ?? TimelineBar.StatusText(b.Status), b.Status))
                    .OrderByDescending(g => Severity(g.Key.Status)).ThenBy(g => g.Key.Lifecycle)
                    .Select(g => (g.Key.Lifecycle, g.Count()))
                    .ToList();
                var run = new TimelineBar
                {
                    InstanceId = instanceId,
                    JobId = first.JobId,
                    JobName = jobName,
                    StepId = stepId,
                    StepName = first.StepName,
                    Start = start,
                    End = end,
                    DurationSec = (int)(end - start).TotalSeconds,
                    Status = runBars.Select(b => b.Status).MaxBy(Severity),
                    // One run of the step - its targets are described in the tooltip and counted on the rows beneath
                    Executions = 1,
                    HistoryId = first.HistoryId,
                    SourceIndex = first.SourceIndex,
                    Row = step
                };
                step.Bars.Add(run);
                model.Bars.Add(run);
                info[run] = new BarInfo(null, outcomes[0].Lifecycle, outcomes); /* Status: the worst */
            }

            foreach (var job in model.Roots)
            {
                job.BarsChanged();
                foreach (var step in job.Children)
                {
                    step.BarsChanged();
                    step.Children.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
                    foreach (var target in step.Children) target.BarsChanged();
                }
            }
            return model;
        }

        /// <summary>Worse outcomes rank higher, so a run is coloured by its worst target</summary>
        private static int Severity(RunStatus status) => status switch
        {
            RunStatus.Failed => 5,
            RunStatus.Retry => 4,
            RunStatus.Cancelled => 3,
            RunStatus.InProgress => 2,
            _ => 1
        };

        /// <summary>
        /// The row label for a target, and a full description for the tooltip.  A database by its name (with its server's, where the
        /// step runs against more than one server).  A server or pool is a target group member that was expanded into its databases -
        /// it's only listed where that failed.
        /// </summary>
        internal static (string Label, string Description) TargetName(DataRow r, bool includeServer)
        {
            var server = r["target_server_name"] as string;
            var pool = r.Table.Columns.Contains("target_elastic_pool_name") ? r["target_elastic_pool_name"] as string : null;
            var database = r["target_database_name"] as string;
            var description = string.Join(" / ", new[] { server, pool, database }.Where(s => !string.IsNullOrEmpty(s)));
            if (string.IsNullOrEmpty(description)) description = r.Table.Columns.Contains("target_type") ? r["target_type"] as string ?? "(unknown target)" : "(unknown target)";

            string label;
            if (!string.IsNullOrEmpty(database))
            {
                label = includeServer && !string.IsNullOrEmpty(server) ? ShortServerName(server) + "/" + database : database;
            }
            else if (!string.IsNullOrEmpty(pool))
            {
                label = pool + " (pool)";
            }
            else
            {
                label = string.IsNullOrEmpty(server) ? description : ShortServerName(server) + " (server)";
            }
            return (label, description);
        }

        /// <summary>WaitingForRetry -&gt; Waiting for retry, TimedOut -&gt; Timed out</summary>
        internal static string LifecycleText(string lifecycle) =>
            string.IsNullOrEmpty(lifecycle) ? lifecycle
                : System.Text.RegularExpressions.Regex.Replace(lifecycle, "(?<=[a-z])(?=[A-Z])", " ") is var spaced && spaced.Length > 1
                    ? spaced[..1] + spaced[1..].ToLowerInvariant()
                    : spaced;

        /// <summary>myserver.database.windows.net -&gt; myserver</summary>
        private static string ShortServerName(string server)
        {
            var dot = server.IndexOf('.');
            return dot > 0 ? server[..dot] : server;
        }

        private static TimelineRowModel GetOrAdd<TKey>(Dictionary<TKey, TimelineRowModel> lookup, TKey key, Func<TimelineRowModel> create, List<TimelineRowModel> addTo)
        {
            if (lookup.TryGetValue(key, out var row)) return row;
            row = create();
            lookup.Add(key, row);
            addTo.Add(row);
            return row;
        }

        /// <summary>Tooltip lines for a bar of a step's run or of a target's execution.  Null for other bars (the chart's own details).</summary>
        public static IEnumerable<(string, string)> Details(TimelineBar bar, BarInfo info)
        {
            var lines = new List<(string, string)> { ("Step", bar.StepLabel) };
            if (info.Target != null) lines.Add(("Target", info.Target));
            lines.Add(("Status", info.Lifecycle != null ? LifecycleText(info.Lifecycle) : TimelineBar.StatusText(bar.Status)));
            if (info.TargetOutcomes != null)
            {
                var total = info.TargetOutcomes.Sum(o => o.Count);
                var byOutcome = info.TargetOutcomes.Select(o => $"{o.Count:N0} {LifecycleText(o.Lifecycle).ToLowerInvariant()}");
                lines.Add(("Targets", $"{total:N0}: {string.Join(", ", byOutcome)}"));
            }
            lines.Add(("Start", bar.Start.ToString("G")));
            lines.Add(("End", bar.IsRunning ? "(still running)" : bar.End.ToString("G")));
            lines.Add((bar.IsRunning ? "Elapsed" : "Duration", JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(bar.DurationSec), true)));
            return lines;
        }
    }
}
