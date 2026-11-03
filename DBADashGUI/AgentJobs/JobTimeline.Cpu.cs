using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// The instance's CPU alongside the jobs (dbo.CPU_Get): where it's busy and where it's quiet, for deciding when jobs
    /// should run.  For one instance - in the timeline and in plan mode.  Fetched for the time in view (and as much again
    /// either side) at a grouping of about a column every few pixels.
    /// </summary>
    public partial class JobTimeline
    {
        private ToolStripButton tsCpu;

        /// <summary>What's loaded: the window and grouping, in UTC and minutes</summary>
        private DateTime? cpuFromUtc;

        private DateTime? cpuToUtc;
        private int cpuGroupMins;
        private int cpuGeneration;

        /// <summary>The banner a failed CPU fetch put up - taken down when a later one works</summary>
        private string cpuError;

        /// <summary>Groupings to fetch CPU at, in minutes.  Collected a minute at a time; an hour or more comes from the hourly table.</summary>
        private static readonly int[] CpuGroupings = { 1, 2, 5, 10, 15, 30, 60, 120, 240, 360, 720, 1440 };

        /// <summary>About this many pixels a column</summary>
        private const int CpuPixelsPerGroup = 3;

        private bool CpuAvailable => context != null && !MultiInstance;

        /// <summary>The CPU toggle, at <paramref name="index"/> on the toolbar</summary>
        private void AddCpuItems(int index)
        {
            tsCpu = new ToolStripButton("CPU", Properties.Resources.LineChart_16x)
            {
                CheckOnClick = true,
                ToolTipText = "Show the instance's CPU above the jobs - SQL Server in blue, other processes in grey, and the peak in red"
            };
            tsCpu.CheckedChanged += (_, _) =>
            {
                chart.ShowCpu = tsCpu.Checked && CpuAvailable;
                FetchCpuForView(true);
            };
            toolStrip1.Items.Insert(index, tsCpu);
        }

        /// <summary>Show or hide the toggle for the context: CPU is an instance's</summary>
        private void SetCpuContext(bool sameContext)
        {
            tsCpu.Visible = !MultiInstance;
            chart.ShowCpu = tsCpu.Checked && CpuAvailable;
            if (sameContext) return;
            cpuFromUtc = null;
            chart.SetCpu(null);
        }

        /// <summary>
        /// Fetch CPU for the time in view, unless what's loaded already covers it at about the right grouping.  The window is
        /// the view and as much again either side, within the period.
        /// </summary>
        private async void FetchCpuForView(bool force)
        {
            if (!chart.ShowCpu || chart.ViewTo <= chart.ViewFrom) return;
            if (!ViewToUtc(out var viewFrom, out var viewTo)) return;
            var span = viewTo - viewFrom;
            var target = span.TotalMinutes / Math.Max(1, chart.TimeAxisWidth / CpuPixelsPerGroup);
            var group = CpuGroupings.FirstOrDefault(g => g >= target, CpuGroupings[^1]);
            if (!force && cpuFromUtc <= viewFrom && cpuToUtc >= viewTo && cpuGroupMins == group) return;

            var periodFrom = FromDateUtc;
            var periodTo = ToDateUtc;
            var windowFrom = viewFrom - span < periodFrom ? periodFrom : viewFrom - span;
            var windowTo = viewTo + span > periodTo ? periodTo : viewTo + span;
            if (windowTo <= windowFrom) (windowFrom, windowTo) = (viewFrom, viewTo);
            var instanceId = InstanceId;
            var generation = ++cpuGeneration;
            try
            {
                var samples = await Task.Run(() => GetCpu(instanceId, windowFrom, windowTo, group));
                if (generation != cpuGeneration) return;
                (cpuFromUtc, cpuToUtc, cpuGroupMins) = (windowFrom, windowTo, group);
                chart.SetCpu(samples);
                // Loaded after all: take down the error, unless something else has put up a message since
                if (cpuError != null && chart.Banner == cpuError) chart.Banner = null;
                cpuError = null;
            }
            catch (Exception ex)
            {
                if (generation != cpuGeneration) return;
                cpuError = "Couldn't load the CPU: " + ex.Message;
                chart.Banner = cpuError;
            }
        }

        /// <summary>CPU grouped to <paramref name="groupMins"/> minutes, each group converted to the app time zone</summary>
        private static List<JobTimelineChart.CpuSample> GetCpu(int instanceId, DateTime fromUtc, DateTime toUtc, int groupMins)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CPU_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            cmd.Parameters.Add("@FromDate", SqlDbType.DateTime2).Value = fromUtc;
            cmd.Parameters.Add("@ToDate", SqlDbType.DateTime2).Value = toUtc;
            cmd.Parameters.AddWithValue("@DateGroupingMin", groupMins);
            var dt = new DataTable();
            da.Fill(dt);
            return dt.Rows.Cast<DataRow>()
                .Select(r =>
                {
                    var startUtc = (DateTime)r["EventTime"];
                    return new JobTimelineChart.CpuSample(startUtc.ToAppTimeZone(), startUtc.AddMinutes(groupMins).ToAppTimeZone(),
                        Convert.ToSingle(r["SQLProcessCPU"]), Convert.ToSingle(r["OtherCPU"]), Convert.ToSingle(r["MaxCPU"]));
                })
                .ToList();
        }
    }
}
