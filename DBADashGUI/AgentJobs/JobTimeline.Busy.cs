using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// Busy hours on the job timeline: fetched with the timeline's data for a single instance and shaded behind the runs, a
    /// toggle to hide them, and the editor (<see cref="BusyPeriodsForm"/>).  In plan mode the editor gets them in UTC and
    /// shades them itself, in the plan's time.
    /// </summary>
    public partial class JobTimeline
    {
        private ToolStripDropDownButton tsBusy;
        private ToolStripMenuItem tsShowBusy;
        private ToolStripMenuItem tsEditBusy;

        /// <summary>The busy intervals for the period loaded, in UTC.  Empty on the multi-instance timeline.</summary>
        private List<(DateTime Start, DateTime End)> busyUtc = new();

        /// <summary>The Busy Hours drop-down: show or hide the shading, and edit the periods</summary>
        private void AddBusyItems(int index)
        {
            tsBusy = new ToolStripDropDownButton("Busy Hours")
            {
                ToolTipText = "When the instance is busy - e.g. weekdays 08:00 to 18:00 - shaded behind the runs.  Set for the instance, a tag or all instances.  In plan mode, jobs that could run outside them are kept out of them."
            };
            tsShowBusy = new ToolStripMenuItem("Show Busy Hours") { CheckOnClick = true, Checked = true };
            tsShowBusy.CheckedChanged += (_, _) => chart.ShowBusyPeriods = tsShowBusy.Checked;
            tsEditBusy = new ToolStripMenuItem("Busy Hours...", null, (_, _) => EditBusyHours())
            {
                ToolTipText = "Add, edit, delete or copy the instance's busy periods"
            };
            tsBusy.DropDownItems.AddRange(new ToolStripItem[] { tsShowBusy, tsEditBusy });
            toolStrip1.Items.Insert(index, tsBusy);
        }

        /// <summary>
        /// The busy intervals for one instance over the range, in UTC - off the UI thread.  A failure (e.g. a repository
        /// that predates busy periods) leaves them off rather than failing the timeline, and says so on the button.
        /// </summary>
        private Task<List<(DateTime Start, DateTime End)>> FetchBusyUtc(int instanceId, DateTime fromUtc, DateTime toUtc) =>
            Task.Run(() =>
            {
                try
                {
                    return BusyPeriodsData.Get(instanceId, fromUtc, toUtc).IntervalsUtc;
                }
                catch (Exception ex)
                {
                    BeginInvoke(() => tsBusy.ToolTipText = "Couldn't load the busy hours: " + ex.Message);
                    return new List<(DateTime Start, DateTime End)>();
                }
            });

        /// <summary>The busy hours on the timeline, in the app time zone like the runs</summary>
        private void ShowBusyOnTimeline()
        {
            if (PlanMode) return;
            chart.BusyPeriods = MultiInstance ? null : busyUtc.Select(b => (b.Start.ToAppTimeZone(), b.End.ToAppTimeZone())).ToList();
        }

        /// <summary>
        /// The range to fetch busy hours over in plan mode: the selected period and the predicted preview, which runs up to
        /// 7 days from midnight today - and a day either side for moves and the server's offset
        /// </summary>
        private static (DateTime FromUtc, DateTime ToUtc) PlanBusyRange(DateTime fromUtc, DateTime toUtc)
        {
            var now = DateTime.UtcNow;
            return ((fromUtc < now ? fromUtc : now).AddDays(-2), (toUtc > now ? toUtc : now).AddDays(9));
        }

        private void EditBusyHours()
        {
            if (context == null || MultiInstance)
            {
                MessageBox.Show("Busy hours are set for an instance - select an instance (they can apply to a tag or all instances from there).", "Busy Hours", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var frm = new BusyPeriodsForm(InstanceId, context.InstanceName);
            frm.ShowDialog(this);
            if (!frm.Changed) return;
            if (PlanMode) LoadPlanBusy();
            else RefreshData();
        }

        /// <summary>Fetch the busy hours for plan mode and give them to the editor - after an edit, without loading the plan again</summary>
        private async void LoadPlanBusy()
        {
            if (planEditor == null) return;
            var (fromUtc, toUtc, _) = PlanPeriodUtc();
            var (busyFrom, busyTo) = PlanBusyRange(fromUtc, toUtc);
            var busy = await FetchBusyUtc(InstanceId, busyFrom, busyTo);
            if (planEditor != null) planEditor.BusyPeriodsUtc = busy;
        }
    }
}
