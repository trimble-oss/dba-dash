using Humanizer;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DBADash.JobSchedules
{
    /// <summary>
    /// A SQL Server Agent schedule as msdb stores it (sysschedules).  Dates are yyyymmdd and times hhmmss integers, in the
    /// server's local time - the same values sp_add_schedule takes.
    /// </summary>
    public sealed record AgentSchedule
    {
        public const int Once = 1;
        public const int Daily = 4;
        public const int Weekly = 8;
        public const int Monthly = 16;
        public const int MonthlyRelative = 32;
        public const int AgentStart = 64;
        public const int Idle = 128;

        public const int SubdayAtTime = 1;
        public const int SubdaySeconds = 2;
        public const int SubdayMinutes = 4;
        public const int SubdayHours = 8;

        public string Name { get; init; }
        public bool Enabled { get; init; } = true;
        public int FrequencyType { get; init; }
        public int FrequencyInterval { get; init; }
        public int FrequencySubdayType { get; init; }
        public int FrequencySubdayInterval { get; init; }
        public int FrequencyRelativeInterval { get; init; }
        public int FrequencyRecurrenceFactor { get; init; }
        public int ActiveStartDate { get; init; } = 19900101;
        public int ActiveEndDate { get; init; } = 99991231;
        public int ActiveStartTime { get; init; }
        public int ActiveEndTime { get; init; } = 235959;

        private readonly string _scheduleUid;

        /// <summary>schedule_uid as collected.  <see cref="Uid"/> is what schedules are matched by.</summary>
        public string ScheduleUid
        {
            get => _scheduleUid;
            init
            {
                _scheduleUid = value;
                Uid = Guid.TryParse(value, out var uid) ? uid : null;
            }
        }

        /// <summary>schedule_uid, parsed once - null when there isn't one, so the schedule can't be found on the server or matched with another job's</summary>
        public Guid? Uid { get; private init; }

        /// <summary>msdb's earliest start date stands in for a missing one</summary>
        public DateTime StartDate => ToDate(ActiveStartDate) ?? new DateTime(1990, 1, 1);

        /// <summary>The last day the schedule can run on (inclusive)</summary>
        public DateTime EndDate => ToDate(ActiveEndDate) ?? DateTime.MaxValue.Date;

        public TimeSpan StartTime => ToTime(ActiveStartTime);
        public TimeSpan EndTime => ToTime(ActiveEndTime);

        /// <summary>Seconds between runs within a day.  0 for a schedule that runs once a day at <see cref="StartTime"/>.</summary>
        public int SubdayStepSeconds => FrequencySubdayType switch
        {
            SubdaySeconds => FrequencySubdayInterval,
            SubdayMinutes => FrequencySubdayInterval * 60,
            SubdayHours => FrequencySubdayInterval * 3600,
            _ => 0
        };

        /// <summary>freq_recurrence_factor - msdb stores 0 for "every" on schedules that ignore it</summary>
        public int RecurrenceFactor => Math.Max(1, FrequencyRecurrenceFactor);

        private static DateTime? ToDate(int date)
        {
            if (date <= 0 || date >= 99991231) return null;
            try
            {
                return new DateTime(date / 10000, date / 100 % 100, date % 100);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static TimeSpan ToTime(int time) => new(time / 10000, time / 100 % 100, time % 100);

        /// <summary>When the schedule runs, as SQL Server Agent's schedule dialog would put it</summary>
        public string Describe()
        {
            var sb = new StringBuilder();
            switch (FrequencyType)
            {
                case Once:
                    return $"Once on {StartDate:d} at {FormatTime(ActiveStartTime)}";

                case Daily:
                    sb.Append($"Daily every {FrequencyInterval} day(s)");
                    break;

                case Weekly:
                    sb.Append($"Every {FrequencyRecurrenceFactor} week(s) on {DaysFromBitMask(FrequencyInterval)}");
                    break;

                case Monthly:
                    sb.Append($"Every {FrequencyRecurrenceFactor} month(s) on day {FrequencyInterval} of month");
                    break;

                case MonthlyRelative:
                    var relative = FrequencyRelativeInterval switch
                    {
                        1 => "first",
                        2 => "second",
                        4 => "third",
                        8 => "fourth",
                        16 => "last",
                        _ => "???"
                    };
                    var day = FrequencyInterval switch
                    {
                        >= 1 and <= 7 => ((DayOfWeek)(FrequencyInterval - 1)).ToString(),
                        8 => "day",
                        9 => "weekday",
                        10 => "weekend day",
                        _ => "(" + FrequencyInterval + "Unknown)",
                    };
                    sb.Append($"{relative.Titleize()} {day} of every {FrequencyRecurrenceFactor} month(s)");
                    break;

                case AgentStart:
                    return "When the SQL Server agent starts";

                case Idle:
                    return "When the CPU is idle";

                default:
                    sb.AppendLine($"Unknown Frequency Type {FrequencyType}");
                    break;
            }

            switch (FrequencySubdayType)
            {
                case SubdayAtTime:
                    sb.Append($", at {FormatTime(ActiveStartTime)}");
                    break;

                case > SubdayAtTime:
                    var unit = FrequencySubdayType switch
                    {
                        SubdaySeconds => "seconds",
                        SubdayMinutes => "minutes",
                        SubdayHours => "hours",
                        _ => $"unknown ({FrequencySubdayType})"
                    };
                    sb.Append($", every {FrequencySubdayInterval} {unit}");
                    break;
            }
            if (FrequencySubdayType != SubdayAtTime) sb.Append($", between {FormatTime(ActiveStartTime)} and {FormatTime(ActiveEndTime)}");
            return sb.ToString();
        }

        /// <summary>The days in a weekly schedule's freq_interval (Sunday = 1 ... Saturday = 64), from the culture's first day of the week</summary>
        public static string DaysFromBitMask(int value)
        {
            var firstDayOfWeek = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            return string.Join(", ", Enumerable.Range(0, 7)
                .Where(d => (value & (1 << d)) != 0)
                .OrderBy(d => (d - firstDayOfWeek + 7) % 7)
                .Select(d => ((DayOfWeek)d).ToString()));
        }

        private static string FormatTime(int time) => $"{time / 10000:D2}:{time / 100 % 100:D2}:{time % 100:D2}";
    }
}
