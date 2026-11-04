using DBADash.JobSchedules;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// Busy hours in plan mode: which runs of jobs that could keep clear of them land in them, before and after the moves -
    /// counted in the grid, outlined in amber on the chart and cautioned about when a move puts a job into them.  Jobs that
    /// run too often to keep clear (<see cref="BusyPeriods.CanAvoid"/>) aren't counted, so they don't nag.
    /// </summary>
    internal sealed partial class SchedulePlanEditor
    {
        private IReadOnlyList<(DateTime Start, DateTime End)> _busyUtc = Array.Empty<(DateTime Start, DateTime End)>();

        /// <summary>The busy intervals in server time, like the schedules and runs - made again when the server's UTC offset changes</summary>
        private BusyPeriods _busy;

        /// <summary>Jobs that could keep clear of busy hours that the moves put into them, for the caution and their notes</summary>
        private HashSet<Guid> _movedIntoBusy = new();

        /// <summary>For each job previewed, which of its runs are in busy hours - null for a job not held to them</summary>
        private List<bool[]> _busyRunFlags = new();

        /// <summary>The Busy Hours button was clicked: the host opens the busy periods editor, and sets <see cref="BusyPeriodsUtc"/> again after</summary>
        public event EventHandler EditBusyHoursRequested;

        /// <summary>
        /// When the instance is busy, in UTC (dbo.BusyPeriods_Get), covering the selected range and the predicted preview -
        /// the host fetches them with the plan's data
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<(DateTime Start, DateTime End)> BusyPeriodsUtc
        {
            get => _busyUtc;
            set
            {
                _busyUtc = value ?? Array.Empty<(DateTime Start, DateTime End)>();
                _busy = null;
                RefreshWhatIf();
            }
        }

        /// <summary>The busy intervals in server time: UTC less the server's offset, as <see cref="ToUtc"/> adds it</summary>
        private BusyPeriods Busy => _busy ??= _busyUtc.Count == 0
            ? BusyPeriods.None
            : new BusyPeriods(_busyUtc.Select(b => new BusyInterval(b.Start.AddMinutes(-_whatIfUtcOffset), b.End.AddMinutes(-_whatIfUtcOffset))));

        private ToolStripItem CreateBusyItem() =>
            new ToolStripButton("Busy Hours...", null, (_, _) => EditBusyHoursRequested?.Invoke(this, EventArgs.Empty))
            {
                ToolTipText = "When the instance is busy - e.g. weekdays 08:00 to 18:00.  Shaded on the chart; runs of jobs that could run outside them (daily, weekly, or repeating less often than the busy stretch) are outlined in amber when they're in them.  " +
                              "Jobs that run too often to keep clear aren't counted."
            };

        /// <summary>The Busy Hours column's runs, and the change from before the moves - for a job held to busy hours</summary>
        private void SetBusyCells(DataRow r, JobScheduleInput job, bool included, bool anyMoves)
        {
            if (!included || !Busy.CanAvoid(job))
            {
                r["BusyRuns"] = DBNull.Value;
                r["BusyRunsChange"] = DBNull.Value;
                r["BusyDetail"] = included && !Busy.IsEmpty ? "Runs too often to keep clear of busy hours, so it isn't counted" : DBNull.Value;
                return;
            }
            var after = _plan.InBusyHours(job, Busy, true, _rangeFrom, _rangeTo);
            var before = anyMoves ? _plan.InBusyHours(job, Busy, false, _rangeFrom, _rangeTo) : after;
            r["BusyRuns"] = after.Runs;
            r["BusyRunsChange"] = after.Runs - before.Runs;
            r["BusyDetail"] = $"Runs in busy hours over the range: {after.Runs:N0} ({JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(after.Seconds))})" +
                              (anyMoves ? $", was {before.Runs:N0} ({JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(before.Seconds))}) before the moves" : "");
        }

        /// <summary>Which previewed runs are in busy hours, for the amber outlines - and how many</summary>
        private int FlagBusyRuns(List<JobScheduleInput> included, List<IReadOnlyList<JobRun>> runs)
        {
            _busyRunFlags = new List<bool[]>(runs.Count);
            var count = 0;
            for (var i = 0; i < runs.Count; i++)
            {
                if (!Busy.CanAvoid(included[i]))
                {
                    _busyRunFlags.Add(null);
                    continue;
                }
                var flags = new bool[runs[i].Count];
                count += Busy.Measure(runs[i], flags).Runs;
                _busyRunFlags.Add(flags);
            }
            return count;
        }

        private bool RunInBusy(int job, int run) => job < _busyRunFlags.Count && _busyRunFlags[job] is { } flags && run < flags.Length && flags[run];

        private bool AnyRunInBusy(int job, int first, int end)
        {
            for (var k = first; k < end; k++)
            {
                if (RunInBusy(job, k)) return true;
            }
            return false;
        }

        /// <summary>The busy hours shaded on the chart, converted as the runs are</summary>
        private void ShowBusyOnChart()
        {
            if (_preview == null) return;
            _preview.BusyPeriods = Busy.Intervals.Select(b => (Display(b.Start), Display(b.End))).ToList();
        }

        /// <summary>Runs in busy hours for the summary: "3 runs of jobs that could avoid them in busy hours (was 5)."</summary>
        private string BusySummary(List<JobScheduleInput> included, bool anyMoves)
        {
            if (Busy.IsEmpty) return "";
            var holding = included.Where(Busy.CanAvoid).ToList();
            if (holding.Count == 0) return "No previewed job could keep clear of busy hours, so none are held to them.  ";
            var after = holding.Sum(j => _plan.InBusyHours(j, Busy, true, _rangeFrom, _rangeTo).Runs);
            var before = anyMoves ? holding.Sum(j => _plan.InBusyHours(j, Busy, false, _rangeFrom, _rangeTo).Runs) : after;
            return $"{after:N0} runs of jobs that could avoid them in busy hours" + (anyMoves && before != after ? $" (was {before:N0})" : "") + ".  ";
        }

        /// <summary>Jobs that could keep clear of busy hours that the moves put into them</summary>
        private void FindMovedIntoBusy() =>
            _movedIntoBusy = Busy.IsEmpty ? new HashSet<Guid>() : _plan.MovedIntoBusyHours(Busy, _rangeFrom, _rangeTo).Select(j => j.JobId).ToHashSet();
    }
}
