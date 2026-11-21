using DBADashGUI.AgentJobs;
using DBADashGUI.Performance;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.ElasticJobs
{
    /// <summary>
    /// Timeline of elastic job executions, drawn with the SQL Agent job timeline chart.  dbo.ElasticJobTimeline_Get returns the same
    /// columns as dbo.JobTimeline_Get, so the chart takes them as it does SQL Agent jobs.  With steps shown, a step has a bar per run
    /// across its targets, and a row per target beneath it - see <see cref="ElasticJobTimelineModel"/>.  Where executions are merged
    /// (a very busy period), steps are a row each without their targets.
    ///
    /// <para>Activating a bar shows that execution: for the job, its executions against each target.  The chart identifies a single
    /// execution by a history ID (an int, from SQL Agent job history), so the row's index in the data is used as one and mapped back to
    /// the execution here.</para>
    /// </summary>
    internal sealed class ElasticJobTimeline : UserControl, ISetContext, IRefreshData
    {
        /// <summary>
        /// Executions are merged into spans of bars too close together to tell apart beyond this many - a job that runs every minute
        /// against hundreds of databases, over a long period.  A merged bar has no single execution to show.
        /// </summary>
        private const int MergeAboveRows = 20000;

        private readonly JobTimelineChart chart;
        private readonly ToolStripButton tsSteps;
        private readonly ToolStripButton tsProblemsOnly;
        private readonly ToolStripTextBox tsFilter;
        private DBADashContext context;
        private DataTable data;
        private JobTimelineModel model;
        private Dictionary<TimelineBar, ElasticJobTimelineModel.BarInfo> barInfo = new();
        private bool resetView = true;
        private CancellationTokenSource loadCancellation;

        public ElasticJobTimeline()
        {
            chart = new JobTimelineChart
            {
                Dock = DockStyle.Fill,
                EmptyText = "No elastic job executions in the selected period",
                BarActivatedText = "Execution details"
            };
            chart.BarActivated += Chart_BarActivated;
            chart.JobActivated += Chart_JobActivated;
            // A run of a step says how its targets went, and a target's execution which target it was
            chart.BarDetailsProvider = bar => barInfo.TryGetValue(bar, out var info) ? ElasticJobTimelineModel.Details(bar, info) : null;

            var toolStrip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            var tsRefresh = new ToolStripButton("Refresh", Properties.Resources._112_RefreshArrow_Green_16x16_72) { DisplayStyle = ToolStripItemDisplayStyle.Image };
            tsRefresh.Click += (_, _) => RefreshData();
            tsSteps = new ToolStripButton("Steps", Properties.Resources.List_NumberedHS)
            {
                CheckOnClick = true,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ToolTipText = "Show each job's steps beneath it - a bar per run of the step.  Expand a step for a row per target."
            };
            tsSteps.CheckedChanged += (_, _) => RefreshData();
            tsProblemsOnly = new ToolStripButton("Problems Only", Properties.Resources.Filter_16x)
            {
                CheckOnClick = true,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ToolTipText = "Only executions that failed, were retried, canceled or skipped"
            };
            tsProblemsOnly.CheckedChanged += (_, _) => RefreshData();
            tsFilter = new ToolStripTextBox { ToolTipText = "Filter jobs" };
            tsFilter.TextBox.PlaceholderText = "Filter jobs";
            tsFilter.TextChanged += (_, _) => chart.Filter = tsFilter.Text;
            var tsCopy = new ToolStripButton("Copy", Properties.Resources.ASX_Copy_blue_16x) { DisplayStyle = ToolStripItemDisplayStyle.Image, ToolTipText = "Copy image" };
            tsCopy.Click += (_, _) => chart.CopyImage();
            var tsResetZoom = new ToolStripButton("Reset Zoom") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            tsResetZoom.Click += (_, _) => chart.ResetZoom();
            toolStrip.Items.AddRange(new ToolStripItem[] { tsRefresh, tsCopy, new ToolStripSeparator(), tsSteps, tsProblemsOnly, tsFilter, tsResetZoom });

            Controls.Add(chart);
            Controls.Add(toolStrip);
            Disposed += (_, _) => loadCancellation?.Cancel();
        }

        public void SetContext(DBADashContext _context)
        {
            resetView = context == null || context.InstanceID != _context.InstanceID;
            context = _context;
            RefreshData();
        }

        public void RefreshData()
        {
            if (context == null) return;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            loadCancellation?.Cancel();
            var cts = new CancellationTokenSource();
            loadCancellation = cts;
            var instanceId = context.InstanceID;
            var fromUtc = DateRange.FromUTC;
            var toUtc = DateRange.ToUTC;
            var steps = tsSteps.Checked;
            var problemsOnly = tsProblemsOnly.Checked;
            try
            {
                // Merged on the server only if there are more than MergeAboveRows executions - one call either way, rather than fetching them
                // all to find out.  Merged bars are a few pixels wide at most - the narrowest the chart draws.
                var mergeGapSec = Math.Max(1, (int)((toUtc - fromUtc).TotalSeconds / Math.Max(chart.ChartPixelWidth, 500) * 3));
                var dt = await Task.Run(() => GetTimelineData(instanceId, fromUtc, toUtc, steps, problemsOnly, mergeGapSec, MergeAboveRows), cts.Token);
                if (cts.IsCancellationRequested || IsDisposed) return;

                var info = new Dictionary<TimelineBar, ElasticJobTimelineModel.BarInfo>();
                JobTimelineModel newModel;
                if (steps && ElasticJobTimelineModel.HasTargets(dt))
                {
                    // Steps as a bar per run, with a row per target beneath.  Steps the user expanded stay expanded.
                    var stepCollapsed = (model?.Roots ?? Enumerable.Empty<TimelineRowModel>())
                        .SelectMany(job => job.Children)
                        .GroupBy(ElasticJobTimelineModel.StepKey)
                        .ToDictionary(g => g.Key, g => g.First().Collapsed);
                    newModel = ElasticJobTimelineModel.Build(dt, instanceId, stepCollapsed, info);
                }
                else
                {
                    // The chart's ID for a single execution: the row it came from (see the class summary)
                    for (var i = 0; i < dt.Rows.Count; i++)
                    {
                        if ((int)dt.Rows[i]["Executions"] == 1 && dt.Rows[i]["job_execution_id"] != DBNull.Value)
                        {
                            dt.Rows[i]["instance_id"] = i;
                        }
                    }
                    newModel = JobTimelineModel.Build(dt, false, steps, instanceId);
                }
                data = dt;
                model = newModel;
                barInfo = info;
                chart.Title = context.InstanceName;
                chart.Banner = dt.Columns.Contains("FailedRuns") ? "Executions close together are combined.  Select a shorter period to see each one." : null;
                chart.SetData(newModel, fromUtc.ToAppTimeZone(), toUtc.ToAppTimeZone(), resetView);
                resetView = false;
            }
            catch (OperationCanceledException)
            {
                // Superseded by a later load
            }
            catch (Exception ex)
            {
                if (cts.IsCancellationRequested || IsDisposed) return;
                chart.Banner = "Error loading the timeline: " + ex.Message;
            }
        }

        /// <summary>Timeline data with dates converted to the app time zone</summary>
        /// <param name="mergeGapSec">Merge executions closer together than this, where there are more than <paramref name="maxRows"/></param>
        private static DataTable GetTimelineData(int instanceId, DateTime fromUtc, DateTime toUtc, bool steps, bool problemsOnly, int mergeGapSec, int maxRows)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.ElasticJobTimeline_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            cmd.Parameters.AddWithValue("@FromDate", fromUtc);
            cmd.Parameters.AddWithValue("@ToDate", toUtc);
            cmd.Parameters.AddWithValue("@IncludeSteps", steps);
            cmd.Parameters.AddWithValue("@ProblemsOnly", problemsOnly);
            cmd.Parameters.AddWithValue("@MergeGapSec", mergeGapSec);
            cmd.Parameters.AddWithValue("@MaxRows", maxRows);
            var dt = new DataTable();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        /// <summary>
        /// Show the execution the bar is for.  For the job, its executions against each target.  For a step's execution against a
        /// target, the rows with its execution ID - the whole job execution, where the rows of an execution share its ID.
        /// </summary>
        private void Chart_BarActivated(object sender, TimelineBar bar)
        {
            if (data == null || bar.HistoryId is not { } index || index < 0 || index >= data.Rows.Count) return;
            try
            {
                var jobExecutionId = (Guid)data.Rows[index]["job_execution_id"];
                ElasticJobHistoryReport.ShowExecution(context, bar.InstanceId, bar.JobId, jobExecutionId, this);
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
                ElasticJobHistoryReport.ShowHistory(context, bar.InstanceId, bar.JobId);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }
    }
}
