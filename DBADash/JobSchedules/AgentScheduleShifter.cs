using System;
using System.Collections.Generic;

namespace DBADash.JobSchedules
{
    /// <summary>
    /// Moves a schedule's runs earlier or later by changing its start (and end) time - the change a what-if preview or an
    /// optimiser proposes, and that sp_update_schedule can make.
    ///
    /// <para>A schedule that repeats all day at an interval that divides a day keeps the same runs whatever its start time
    /// within one interval, so it's moved by setting the start time to the earliest equivalent one and the end to 23:59:59.
    /// A shift of 25 minutes on an every-10-minutes schedule is the same as 5.</para>
    /// <para>Moves that would take runs past midnight are refused where that would change the days the schedule runs on
    /// (every n days, weekly on some days, monthly) or the run window's shape.</para>
    /// </summary>
    public static class AgentScheduleShifter
    {
        private const int SecondsPerDay = 86400;
        private const int LastSecond = SecondsPerDay - 1;

        /// <summary>
        /// A schedule moving a job moves: enabled, and runs at times of day.  Agent start and CPU idle schedules have no time
        /// to move, and a one-time schedule is left as it is - so a job's move leaves them, and the runs they start, alone.
        /// </summary>
        public static bool IsMovable(AgentSchedule schedule) =>
            schedule.Enabled && schedule.FrequencyType != AgentSchedule.Once && AgentScheduleProjector.IsProjectable(schedule, out _);

        /// <param name="schedule">The schedule to move</param>
        /// <param name="seconds">How far to move its runs: positive is later</param>
        /// <param name="shifted">The schedule with its new start and end time - the original when it can't be moved</param>
        /// <param name="reason">Why it can't be moved.  Null when it can.</param>
        public static bool TryShift(AgentSchedule schedule, int seconds, out AgentSchedule shifted, out string reason)
        {
            shifted = schedule;
            reason = null;
            if (seconds == 0) return true;
            if (!AgentScheduleProjector.IsProjectable(schedule, out reason)) return false;
            if (schedule.FrequencyType == AgentSchedule.Once)
            {
                reason = "One-time schedule";
                return false;
            }

            var start = (int)schedule.StartTime.TotalSeconds;
            var end = (int)schedule.EndTime.TotalSeconds;
            var everyDay = IsEveryDay(schedule);
            var step = schedule.SubdayStepSeconds;

            if (step > 0)
            {
                var wraps = end < start;
                var endRaw = wraps ? end + SecondsPerDay : end;
                if (RepeatsAllDay(schedule) is not null)
                {
                    shifted = schedule with { ActiveStartTime = ToInt(ScheduleMath.Mod(start + seconds, step)), ActiveEndTime = ToInt(LastSecond) };
                    return true;
                }
                var newStart = start + seconds;
                var newEndRaw = endRaw + seconds;
                var endLimit = wraps ? 2 * SecondsPerDay : SecondsPerDay;
                if (newStart < 0 || newStart >= SecondsPerDay || newEndRaw >= endLimit || (wraps && newEndRaw < SecondsPerDay))
                {
                    reason = "It would move the run window past midnight";
                    return false;
                }
                shifted = schedule with { ActiveStartTime = ToInt(newStart), ActiveEndTime = ToInt(newEndRaw % SecondsPerDay) };
                return true;
            }

            var newTime = start + seconds;
            if (newTime is < 0 or >= SecondsPerDay)
            {
                if (!everyDay)
                {
                    reason = "Moving it past midnight would change the day it runs on";
                    return false;
                }
                newTime = ScheduleMath.Mod(newTime, SecondsPerDay);
            }
            shifted = schedule with { ActiveStartTime = ToInt(newTime) };
            return true;
        }

        /// <summary>
        /// The interval, in seconds, of a schedule that repeats all day every day at an interval that divides a day - whose
        /// runs a move of a whole interval leaves the same, so it's moved by setting its start to the earliest equivalent.
        /// Null for any other schedule.
        /// </summary>
        public static int? RepeatsAllDay(AgentSchedule schedule)
        {
            var step = schedule.SubdayStepSeconds;
            if (step <= 0 || SecondsPerDay % step != 0 || !IsEveryDay(schedule)) return null;
            var start = (int)schedule.StartTime.TotalSeconds;
            var end = (int)schedule.EndTime.TotalSeconds;
            var endRaw = end < start ? end + SecondsPerDay : end;
            return endRaw - start >= SecondsPerDay - step ? step : null;
        }

        /// <summary>
        /// The smallest move that leaves <paramref name="schedules"/> running at the same times as <paramref name="seconds"/>
        /// does - between minus and plus half their common interval, when they all repeat all day (<see cref="RepeatsAllDay"/>).
        /// Every 15 minutes, +20m is +5m and -10m is +5m.  <paramref name="seconds"/> as it is otherwise.
        /// </summary>
        public static int EquivalentShift(IReadOnlyCollection<AgentSchedule> schedules, int seconds)
        {
            if (schedules.Count == 0) return seconds;
            long period = 1;
            foreach (var schedule in schedules)
            {
                if (RepeatsAllDay(schedule) is not { } step) return seconds;
                period = period / ScheduleMath.Gcd(period, step) * step;
            }
            // A common interval divides a day, as each one does
            var s = (int)(((seconds % period) + period) % period);
            return s > period / 2 ? s - (int)period : s;
        }

        /// <summary>Runs every day: daily every 1 day, or weekly every week on all 7 days</summary>
        public static bool IsEveryDay(AgentSchedule schedule) =>
            schedule.FrequencyType == AgentSchedule.Daily && Math.Max(1, schedule.FrequencyInterval) == 1
            || schedule.FrequencyType == AgentSchedule.Weekly && schedule.RecurrenceFactor == 1 && (schedule.FrequencyInterval & 127) == 127;

        /// <summary>Seconds from midnight as msdb's hhmmss integer</summary>
        public static int ToInt(int secondsOfDay) => secondsOfDay / 3600 * 10000 + secondsOfDay / 60 % 60 * 100 + secondsOfDay % 60;
    }
}
