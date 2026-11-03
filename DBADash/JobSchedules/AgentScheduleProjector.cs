using System;
using System.Collections.Generic;

namespace DBADash.JobSchedules
{
    /// <summary>
    /// Works out when a SQL Server Agent schedule fires, from its msdb definition.  Everything is in the server's local
    /// time, as the schedule is.
    ///
    /// <para>A daily window that ends before it starts (every 15 minutes from 22:00 to 02:00) is taken to run through
    /// midnight, the runs after midnight belonging to the day the window opened.</para>
    /// <para>Weeks for "every n weeks" are counted Sunday to Saturday from the week of the schedule's start date, and
    /// months for "every n months" from the month it starts.  A monthly schedule for a day the month doesn't have (the
    /// 31st in April) doesn't run that month.</para>
    /// </summary>
    public static class AgentScheduleProjector
    {
        /// <summary>A Sunday.  Cycle offsets are measured from here, so offsets of different schedules compare.</summary>
        public static readonly DateTime Epoch = new(2000, 1, 2);

        private const long SecondsPerDay = 86400;
        private const long SecondsPerWeek = SecondsPerDay * 7;

        /// <summary>Whether the schedule runs at times that can be worked out in advance, and if not, why</summary>
        public static bool IsProjectable(AgentSchedule schedule, out string reason)
        {
            reason = schedule.FrequencyType switch
            {
                AgentSchedule.AgentStart => "Runs when SQL Server Agent starts",
                AgentSchedule.Idle => "Runs when the CPU is idle",
                AgentSchedule.Once or AgentSchedule.Daily or AgentSchedule.Weekly or AgentSchedule.Monthly or AgentSchedule.MonthlyRelative => null,
                _ => $"Unknown frequency type {schedule.FrequencyType}"
            };
            if (reason != null) return false;
            if (schedule.FrequencyType == AgentSchedule.Once) return true;
            switch (schedule.FrequencySubdayType)
            {
                case AgentSchedule.SubdayAtTime:
                    return true;
                case AgentSchedule.SubdaySeconds or AgentSchedule.SubdayMinutes or AgentSchedule.SubdayHours when schedule.FrequencySubdayInterval > 0:
                    return true;
                default:
                    reason = $"Unknown sub-day frequency {schedule.FrequencySubdayType} every {schedule.FrequencySubdayInterval}";
                    return false;
            }
        }

        /// <summary>
        /// The times the schedule fires from <paramref name="from"/> (inclusive) to <paramref name="to"/> (exclusive), in
        /// order.  Nothing for a disabled schedule or one that can't be projected.
        /// </summary>
        public static IEnumerable<DateTime> RunTimes(AgentSchedule schedule, DateTime from, DateTime to)
        {
            if (!schedule.Enabled || !IsProjectable(schedule, out _) || to <= from) yield break;

            if (schedule.FrequencyType == AgentSchedule.Once)
            {
                var once = schedule.StartDate + schedule.StartTime;
                if (once >= from && once < to) yield return once;
                yield break;
            }

            var times = DayTimes(schedule);
            // A window running through midnight puts the previous day's late runs into this one
            var firstDay = Max(from.Date.AddDays(-1), schedule.StartDate);
            var lastDay = Min(to.Date, schedule.EndDate);
            for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
            {
                if (!DayMatches(schedule, day)) continue;
                foreach (var time in times)
                {
                    var run = day + time;
                    if (run >= to) break;
                    if (run >= from) yield return run;
                }
            }
        }

        /// <summary>
        /// The last time the schedule fires at or before <paramref name="time"/>, looking back no further than the previous
        /// day - worked out rather than listed, so it's cheap to ask for every run of a job that runs every few seconds.
        /// Null when it doesn't fire in that time, or is disabled or can't be projected.  The same times as
        /// <see cref="RunTimes"/>.
        /// </summary>
        public static DateTime? LatestRunAtOrBefore(AgentSchedule schedule, DateTime time)
        {
            if (!schedule.Enabled || !IsProjectable(schedule, out _)) return null;
            if (schedule.FrequencyType == AgentSchedule.Once)
            {
                var once = schedule.StartDate + schedule.StartTime;
                return once <= time && once >= time.Date.AddDays(-1) ? once : null;
            }
            var start = (long)schedule.StartTime.TotalSeconds;
            var step = schedule.SubdayStepSeconds;
            var end = start;
            if (step > 0)
            {
                end = (long)schedule.EndTime.TotalSeconds;
                if (end < start) end += SecondsPerDay;
            }
            // A day's runs are from its start to its end - into the next day for a window through midnight
            for (var day = time.Date; day >= time.Date.AddDays(-1); day = day.AddDays(-1))
            {
                if (day < schedule.StartDate || day > schedule.EndDate || !DayMatches(schedule, day)) continue;
                var offset = (long)Math.Floor((time - day).TotalSeconds);
                if (offset < start) continue;
                var at = step > 0 ? start + Math.Min((offset - start) / step, (end - start) / step) * step : start;
                // Today's latest is always later than yesterday's
                return day.AddSeconds(at);
            }
            return null;
        }

        /// <summary>Times of day the schedule runs on a day it runs, from the start of that day.  Past 24h for a window through midnight.</summary>
        private static List<TimeSpan> DayTimes(AgentSchedule schedule)
        {
            var start = (long)schedule.StartTime.TotalSeconds;
            var step = schedule.SubdayStepSeconds;
            if (step <= 0) return new List<TimeSpan> { TimeSpan.FromSeconds(start) };
            var end = (long)schedule.EndTime.TotalSeconds;
            if (end < start) end += SecondsPerDay;
            var times = new List<TimeSpan>();
            for (var t = start; t <= end; t += step)
            {
                times.Add(TimeSpan.FromSeconds(t));
            }
            return times;
        }

        private static bool DayMatches(AgentSchedule schedule, DateTime day)
        {
            switch (schedule.FrequencyType)
            {
                case AgentSchedule.Daily:
                    return (day - schedule.StartDate).Days % Math.Max(1, schedule.FrequencyInterval) == 0;

                case AgentSchedule.Weekly:
                    if ((schedule.FrequencyInterval & DayBit(day.DayOfWeek)) == 0) return false;
                    var weeks = (WeekStart(day) - WeekStart(schedule.StartDate)).Days / 7;
                    return weeks % schedule.RecurrenceFactor == 0;

                case AgentSchedule.Monthly:
                    return day.Day == schedule.FrequencyInterval && MonthsFromStart(schedule, day) % schedule.RecurrenceFactor == 0;

                case AgentSchedule.MonthlyRelative:
                    return MonthsFromStart(schedule, day) % schedule.RecurrenceFactor == 0 && RelativeDay(schedule, day.Year, day.Month) == day;

                default:
                    return false;
            }
        }

        /// <summary>Bit for the day in a weekly schedule's freq_interval (Sunday = 1 ... Saturday = 64)</summary>
        public static int DayBit(DayOfWeek day) => 1 << (int)day;

        private static DateTime WeekStart(DateTime day) => day.Date.AddDays(-(int)day.DayOfWeek);

        private static int MonthsFromStart(AgentSchedule schedule, DateTime day) =>
            (day.Year * 12 + day.Month) - (schedule.StartDate.Year * 12 + schedule.StartDate.Month);

        /// <summary>
        /// The day a monthly relative schedule picks in a month: the first/second/third/fourth/last
        /// (freq_relative_interval 1/2/4/8/16) Sunday..Saturday (freq_interval 1-7), day (8), weekday (9) or weekend day (10).
        /// </summary>
        private static DateTime? RelativeDay(AgentSchedule schedule, int year, int month)
        {
            var candidates = new List<DateTime>();
            var days = DateTime.DaysInMonth(year, month);
            for (var d = 1; d <= days; d++)
            {
                var date = new DateTime(year, month, d);
                var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                var match = schedule.FrequencyInterval switch
                {
                    >= 1 and <= 7 => (int)date.DayOfWeek == schedule.FrequencyInterval - 1,
                    8 => true,
                    9 => !weekend,
                    10 => weekend,
                    _ => false
                };
                if (match) candidates.Add(date);
            }
            if (candidates.Count == 0) return null;
            var index = schedule.FrequencyRelativeInterval switch
            {
                1 => 0,
                2 => 1,
                4 => 2,
                8 => 3,
                16 => candidates.Count - 1,
                _ => -1
            };
            return index >= 0 && index < candidates.Count ? candidates[index] : null;
        }

        /// <summary>
        /// For a schedule that repeats at a fixed interval: that interval and when in it the schedule runs, in seconds from
        /// <see cref="Epoch"/> modulo the interval.  Covers once a day / every n days, once a week / every n weeks on one
        /// day, and every n seconds/minutes/hours (every day) where n divides a day, so the cadence carries on from one
        /// day to the next.  <paramref name="allDay"/> is false when an every-n schedule runs only part of the day: its
        /// runs are then a subset of the cycle rather than all of it.
        /// </summary>
        public static bool TryGetCycle(AgentSchedule schedule, out long periodSec, out long offsetSec, out bool allDay)
        {
            periodSec = 0;
            offsetSec = 0;
            allDay = true;
            if (!schedule.Enabled || !IsProjectable(schedule, out _)) return false;
            var start = (long)schedule.StartTime.TotalSeconds;
            var step = schedule.SubdayStepSeconds;
            var everyDay = AgentScheduleShifter.IsEveryDay(schedule);

            if (step > 0)
            {
                if (!everyDay || SecondsPerDay % step != 0) return false;
                periodSec = step;
                offsetSec = start % step;
                var end = (long)schedule.EndTime.TotalSeconds;
                if (end < start) end += SecondsPerDay;
                // The last run of the day is followed by the first of the next one a step later
                allDay = end - start >= SecondsPerDay - step;
                return true;
            }

            switch (schedule.FrequencyType)
            {
                case AgentSchedule.Daily:
                    var n = Math.Max(1, schedule.FrequencyInterval);
                    periodSec = n * SecondsPerDay;
                    offsetSec = ScheduleMath.Mod((schedule.StartDate - Epoch).Days, n) * SecondsPerDay + start;
                    return true;

                case AgentSchedule.Weekly when everyDay:
                    periodSec = SecondsPerDay;
                    offsetSec = start;
                    return true;

                case AgentSchedule.Weekly when IsSingleDay(schedule.FrequencyInterval & 127, out var dayOfWeek):
                    var f = schedule.RecurrenceFactor;
                    periodSec = f * SecondsPerWeek;
                    var weeks = (WeekStart(schedule.StartDate) - Epoch).Days / 7;
                    offsetSec = ScheduleMath.Mod(weeks, f) * SecondsPerWeek + dayOfWeek * SecondsPerDay + start;
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsSingleDay(int mask, out int dayOfWeek)
        {
            dayOfWeek = -1;
            if (mask == 0 || (mask & (mask - 1)) != 0) return false;
            dayOfWeek = System.Numerics.BitOperations.TrailingZeroCount(mask);
            return true;
        }

        private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
    }
}
