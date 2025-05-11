using Humanizer;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DBADashGUI.AgentJobs
{
    public class SqlJobSchedule
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public int FrequencyType { get; set; }
        public int FrequencyInterval { get; set; }
        public int FrequencySubdayType { get; set; }
        public int FrequencySubdayInterval { get; set; }
        public int FrequencyRelativeInterval { get; set; }
        public int FrequencyRecurrenceFactor { get; set; }
        public int ActiveStartDate { get; set; }
        public int ActiveEndDate { get; set; }
        public int ActiveStartTime { get; set; }
        public int ActiveEndTime { get; set; }
        public string ScheduleUid { get; set; }

        private string FrequencySubdayTypeDescription => FrequencySubdayType switch
        {
            1 => "At the specified time",
            2 => "Seconds",
            4 => "Minutes",
            8 => "Hours",
            _ => $"Unknown ({FrequencySubdayType})"
        };

        [Flags]
        private enum WeekDayBitMask
        {
            Sunday = 1,
            Monday = 2,
            Tuesday = 4,
            Wednesday = 8,
            Thursday = 16,
            Friday = 32,
            Saturday = 64
        }

        public static string GetDaysFromBitMask(int value)
        {
            var firstDayOfWeek = Convert.ToInt32(CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
            return string.Join(", ", Enum.GetValues(typeof(WeekDayBitMask))
                .Cast<WeekDayBitMask>()
                .Where(day => (value & (int)day) == (int)day)
                .OrderBy(day => GetDaySortOrder(day, firstDayOfWeek))
                .Select(day => day.ToString()));
        }

        private static int GetDaySortOrder(WeekDayBitMask weekday, int firstDayOfWeek)
        {
            var dayOfWeekInt = weekday switch
            {
                WeekDayBitMask.Sunday => 0,
                WeekDayBitMask.Monday => 1,
                WeekDayBitMask.Tuesday => 2,
                WeekDayBitMask.Wednesday => 3,
                WeekDayBitMask.Thursday => 4,
                WeekDayBitMask.Friday => 5,
                WeekDayBitMask.Saturday => 6,
                _ => throw new ArgumentOutOfRangeException(nameof(weekday), weekday, null)
            };
            //Console.WriteLine(dayOfWeekInt);
            return (dayOfWeekInt - (int)firstDayOfWeek + 7) % 7;
        }

        public string GetFrequencyDescription()
        {
            var sb = new StringBuilder();

            switch (FrequencyType)
            {
                case 1:
                    return $"Once on {IntToDate(ActiveStartDate):d} at {FormatTime(ActiveStartTime)}";

                case 4: // Daily
                    sb.Append($"Daily every {FrequencyInterval} day(s)");
                    break;

                case 8: // Weekly
                    sb.Append($"Every {FrequencyRecurrenceFactor} week(s) on {GetDaysFromBitMask(FrequencyInterval)}");
                    break;

                case 16: // Monthly
                    sb.Append($"Every {FrequencyRecurrenceFactor} month(s) on day {FrequencyInterval} of month");
                    break;

                case 32:
                    var frequencyRelative = FrequencyRelativeInterval switch
                    {
                        1 => "first",
                        2 => "second",
                        4 => "third",
                        8 => "fourth",
                        16 => "last",
                        _ => "???"
                    };
                    var interval = FrequencyInterval switch
                    {
                        1 => "Sunday",
                        2 => "Monday",
                        3 => "Tuesday",
                        4 => "Wednesday",
                        5 => "Thursday",
                        6 => "Friday",
                        7 => "Saturday",
                        8 => "day",
                        9 => "weekday",
                        10 => "weekend day",
                        _ => "(" + FrequencyInterval.ToString() + "Unknown)",
                    };

                    sb.Append($"{frequencyRelative.Titleize()} {interval} of every {FrequencyRecurrenceFactor} month(s)");
                    break;

                case 64:
                    return "When the SQL Server agent starts";

                case 128:
                    return "When the CPU is idle";

                default:
                    sb.AppendLine($"Unknown Frequency Type {FrequencyType}");
                    break;
            }

            switch (FrequencySubdayType)
            {
                case 1: // At specified time
                    sb.Append($", at {FormatTime(ActiveStartTime)}");
                    break;

                case > 1:
                    sb.Append($", every {FrequencySubdayInterval} {FrequencySubdayTypeDescription.ToLower()}");
                    break;
            }

            if (FrequencySubdayType != 1) // not at specified time
            {
                sb.Append($", between {FormatTime(ActiveStartTime)} and {FormatTime(ActiveEndTime)}");
            }

            return sb.ToString();
        }

        public DateTime? ScheduleStartDateTime => IntToDate(ActiveStartDate);
        public DateTime? ScheduleEndDateTime => IntToDate(ActiveEndDate);

        private static DateTime? IntToDate(int date)
        {
            if (date == 99991231) return null;
            var year = date / 10000;
            var month = (date % 10000) / 100;
            var day = date % 100;
            return new DateTime(year, month, day);
        }

        private static string FormatTime(int time)
        {
            var hour = time / 10000;
            var minute = (time % 10000) / 100;
            var second = time % 100;

            return $"{hour:D2}:{minute:D2}:{second:D2}";
        }
    }
}