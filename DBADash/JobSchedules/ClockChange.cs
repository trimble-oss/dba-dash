using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.JobSchedules
{
    /// <summary>
    /// The runs before <paramref name="Until"/> (server time as converted; MaxValue for all of them) started
    /// <paramref name="Minutes"/> later in server time than converting them with the instance's UTC offset now says - the
    /// server's clocks changed since.
    /// </summary>
    public readonly record struct ClockChange(DateTime Until, int Minutes)
    {
        /// <summary>A time converted with the offset now, as the server's clock showed it then</summary>
        public DateTime Correct(DateTime converted) => converted < Until ? converted.AddMinutes(Minutes) : converted;
    }

    /// <summary>
    /// Finds a clock change (daylight saving) in the server's time over a set of runs.  DBA Dash keeps when each run started
    /// in UTC and only the instance's UTC offset now, so runs converted back to server time with it are an hour out from
    /// their schedules before a clock change - or all of them, for a period in the other half of the year.
    ///
    /// <para>The runs whose history names the schedule that started them say what the clock showed: each that fits its
    /// schedule as converted, or only an hour later or earlier, is a vote.  A schedule that fits either way (hourly or more
    /// often) says nothing.  A change is taken only when the votes split cleanly in time - an hour out up to a point, as
    /// converted after it - and the hour-out votes come from more than one job, so one schedule moved by an hour isn't
    /// mistaken for the clocks changing.</para>
    /// </summary>
    public static class ClockChanges
    {
        /// <summary>Jobs that must agree the runs are an hour out</summary>
        public const int MinJobs = 2;

        /// <summary>Share of the votes on each side of the change that must agree with it</summary>
        public const double MinAgreement = 0.9;

        private static readonly int[] Hour = { 60, -60 };

        /// <param name="jobs">The jobs, with their schedules as they are</param>
        /// <param name="runs">What each job ran, by job id, converted to server time with the offset now</param>
        public static ClockChange? Find(IReadOnlyList<JobScheduleInput> jobs, IReadOnlyDictionary<Guid, IReadOnlyList<JobRun>> runs)
        {
            var votes = new List<(DateTime Start, int Minutes, Guid JobId)>();
            foreach (var job in jobs)
            {
                if (!runs.TryGetValue(job.JobId, out var jobRuns)) continue;
                foreach (var run in jobRuns)
                {
                    var schedule = SchedulePlan.NamedSchedule(job, run);
                    if (schedule == null || !AgentScheduleShifter.IsMovable(schedule)) continue;
                    var fits = new[] { 0, 60, -60 }.Where(m => SchedulePlan.StartedBy(schedule, run.Start.AddMinutes(m))).ToList();
                    if (fits.Count == 1) votes.Add((run.Start, fits[0], job.JobId));
                }
            }
            if (votes.Count == 0) return null;
            votes.Sort((a, b) => a.Start.CompareTo(b.Start));

            ClockChange? best = null;
            var bestAgreeing = 0;
            foreach (var minutes in Hour)
            {
                // Votes before index b are an hour out, those from b on as converted
                var outBefore = 0;
                var outJobs = new HashSet<Guid>();
                var asConvertedFrom = votes.Count(v => v.Minutes == 0);
                for (var b = 1; b <= votes.Count; b++)
                {
                    if (votes[b - 1].Minutes == minutes)
                    {
                        outBefore++;
                        outJobs.Add(votes[b - 1].JobId);
                    }
                    if (votes[b - 1].Minutes == 0) asConvertedFrom--;
                    var after = votes.Count - b;
                    if (outJobs.Count < MinJobs || outBefore < MinAgreement * b || asConvertedFrom < MinAgreement * after) continue;
                    if (outBefore + asConvertedFrom <= bestAgreeing) continue;
                    bestAgreeing = outBefore + asConvertedFrom;
                    best = new ClockChange(b == votes.Count ? DateTime.MaxValue : votes[b].Start, minutes);
                }
            }
            return best;
        }
    }
}
