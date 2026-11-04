using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.JobSchedules
{
    /// <summary>
    /// Moves a schedule's runs earlier or later by changing its start (and end) time - the change a what-if preview or an
    /// optimiser proposes, and that sp_update_schedule can make.
    ///
    /// <para>A schedule that repeats all day at an interval that divides a day keeps the same runs whatever its start time
    /// within one interval, so it's moved by setting the start time to the earliest equivalent one and the end to 23:59:59.
    /// A shift of 25 minutes on an every-10-minutes schedule is the same as 5.</para>
    /// <para>Moves that take runs past midnight move the days too: every n days moves its start date, weekly moves its days
    /// of the week (Sunday at 00:00 three hours earlier is Saturday at 21:00).  They're refused for monthly schedules,
    /// whose days can't always move with them, for every n weeks when only some days would cross into another week, and
    /// where they would change the run window's shape.</para>
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
        /// <param name="changeDays">Whether it may change the days the schedule runs on - for a move someone makes, not one suggested</param>
        public static bool TryShift(AgentSchedule schedule, int seconds, out AgentSchedule shifted, out string reason, bool changeDays = true)
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
                var days = FloorDiv(start + seconds, SecondsPerDay);
                var newStart = start + seconds - days * SecondsPerDay;
                var newEndRaw = endRaw + seconds - days * SecondsPerDay;
                var endLimit = wraps ? 2 * SecondsPerDay : SecondsPerDay;
                if (newEndRaw >= endLimit || (wraps && newEndRaw < SecondsPerDay) || (days != 0 && !everyDay && !changeDays))
                {
                    reason = "It would move the run window past midnight";
                    return false;
                }
                var timed = schedule with { ActiveStartTime = ToInt(newStart), ActiveEndTime = ToInt(newEndRaw % SecondsPerDay) };
                if (everyDay)
                {
                    shifted = timed;
                    return true;
                }
                return TryMoveDays(timed, days, out shifted, out reason);
            }

            var dayMove = FloorDiv(start + seconds, SecondsPerDay);
            if (dayMove != 0 && !everyDay && !changeDays)
            {
                reason = "Moving it past midnight would change the day it runs on";
                return false;
            }
            var moved = schedule with { ActiveStartTime = ToInt(ScheduleMath.Mod(start + seconds, SecondsPerDay)) };
            if (everyDay)
            {
                shifted = moved;
                return true;
            }
            return TryMoveDays(moved, dayMove, out shifted, out reason);
        }

        /// <summary>
        /// Moves the days <paramref name="schedule"/> runs on by <paramref name="days"/>: every n days by its start (and end)
        /// date, weekly by its days of the week.  Every n weeks counts weeks from the week of its start date, so a day that
        /// crosses into another week moves that week too - which only works when all its days cross: Sunday only, a day
        /// earlier, is Saturday of the week before.
        /// </summary>
        private static bool TryMoveDays(AgentSchedule schedule, int days, out AgentSchedule shifted, out string reason)
        {
            shifted = schedule;
            reason = null;
            if (days == 0) return true;
            var startDate = schedule.StartDate.AddDays(days);
            var endDate = schedule.ActiveEndDate == NoEndDate ? NoEndDate : ToDateInt(schedule.EndDate.AddDays(days));
            switch (schedule.FrequencyType)
            {
                case AgentSchedule.Daily:
                    shifted = schedule with { ActiveStartDate = ToDateInt(startDate), ActiveEndDate = endDate };
                    return true;

                case AgentSchedule.Weekly:
                    if (Math.Abs(days) >= 7)
                    {
                        reason = "It would move it a week or more";
                        return false;
                    }
                    var rotated = 0;
                    var weeks = new HashSet<int>();
                    for (var d = 0; d < 7; d++)
                    {
                        if ((schedule.FrequencyInterval & (1 << d)) == 0) continue;
                        rotated |= 1 << ScheduleMath.Mod(d + days, 7);
                        weeks.Add(FloorDiv(d + days, 7));
                    }
                    if (schedule.RecurrenceFactor > 1)
                    {
                        if (weeks.Count > 1)
                        {
                            reason = $"Only some of its days would move into another week, and it runs every {schedule.RecurrenceFactor} weeks";
                            return false;
                        }
                        var week = WeekStart(schedule.StartDate).AddDays(7 * weeks.Single());
                        if (WeekStart(startDate) != week) startDate = days < 0 ? week.AddDays(6) : week;
                    }
                    shifted = schedule with { FrequencyInterval = rotated, ActiveStartDate = ToDateInt(startDate), ActiveEndDate = endDate };
                    return true;

                default:
                    reason = "Moving it past midnight would change the day of the month it runs on";
                    return false;
            }
        }

        private const int NoEndDate = 99991231;

        private static DateTime WeekStart(DateTime day) => day.Date.AddDays(-(int)day.DayOfWeek);

        private static int FloorDiv(int value, int divisor) => value >= 0 ? value / divisor : -((-value + divisor - 1) / divisor);

        private static int ToDateInt(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;

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
