using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADashGUI.Performance
{
    public static class DateRange
    {
        private static TimeSpan? _selectedTimeSpan = TimeSpan.FromHours(1);
        private static DateTime customFrom = DateTime.MinValue;
        private static DateTime customTo = DateTime.MinValue;
        private static List<int> timeOfDay = new();
        private static List<int> dayOfWeek = new();

        public static bool CurrentDateRangeSupportsTimeOfDayFilter => DurationMins >= 1440;

        public static bool CurrentDateRangeSupportsDayOfWeekFilter => DurationMins >= 10080;

        public static string DateFormatString => TimeSpan.DateFormatString();

        public static TimeSpan? SelectedTimeSpan => _selectedTimeSpan;

        public static void SetTimeSpan(TimeSpan ts)
        {
            _selectedTimeSpan = ts;
            customFrom = DateTime.MinValue;
            customTo = DateTime.MinValue;
            ResetIfNotSupported();
        }

        public static void SetCustom(DateTime fromUTC, DateTime toUTC)
        {
            customFrom = fromUTC;
            customTo = toUTC;
            _selectedTimeSpan = null;
            ResetIfNotSupported();
        }

        private static void ResetIfNotSupported()
        {
            if (!CurrentDateRangeSupportsTimeOfDayFilter)
            {
                ResetTimeOfDayFilter();
            }
            if (!CurrentDateRangeSupportsDayOfWeekFilter)
            {
                ResetDayOfWeekFilter();
            }
        }

        public static void ResetTimeOfDayFilter()
        {
            timeOfDay = new List<int>();
        }

        public static void ResetDayOfWeekFilter()
        {
            dayOfWeek = new List<int>();
        }

        public static bool HasTimeOfDayFilter => timeOfDay.Count is > 0 and < 24;

        public static bool HasDayOfWeekFilter => dayOfWeek.Count is > 0 and < 7;

        public static List<int> TimeOfDay
        {
            get => timeOfDay;
            set
            {
                if (value.Count > 24)
                {
                    throw new Exception("Invalid time of day filter. Expected 24 items or less");
                }
                if (value.Any(hr => hr is > 23 or < 0))
                {
                    throw new Exception("Invalid time of day filter. Expected values 0..23");
                }
                if (value.GroupBy(hr => hr).Any(grp => grp.Count() > 1))
                {
                    throw new Exception("Invalid time of day filter. Duplicate values detected");
                }
                if (!CurrentDateRangeSupportsTimeOfDayFilter)
                {
                    throw new Exception("Time of day filter not supported for this date range");
                }
                timeOfDay = value;
            }
        }

        public static List<int> DayOfWeek
        {
            get => dayOfWeek;
            set
            {
                if (value.Count > 7)
                {
                    throw new Exception("Invalid day of week filter.  Expected 7 items or less");
                }
                if (value.Any(dow => dow is > 7 or < 1))
                {
                    throw new Exception("Invalid days of week filter. Expected values 1..7");
                }
                if (value.GroupBy(dow => dow).Any(grp => grp.Count() > 1))
                {
                    throw new Exception("Invalid day of week filter. Duplicate values detected");
                }
                if (!CurrentDateRangeSupportsDayOfWeekFilter)
                {
                    throw new Exception("Day of week filter is supported for this date range");
                }
                dayOfWeek = value;
            }
        }

        public static DateTime FromUTC
        {
            get
            {
                if (!_selectedTimeSpan.HasValue)
                {
                    return customFrom;
                }
                var utc = DateTime.UtcNow.Subtract(_selectedTimeSpan.Value);
                return _selectedTimeSpan.Value.TotalMinutes > 120 ? new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0) : utc;
            }
        }

        public static DateTime ToUTC => _selectedTimeSpan.HasValue || customTo == DateTime.MaxValue ? DateTime.UtcNow : customTo;

        public static int DurationMins => Convert.ToInt32(TimeSpan.TotalMinutes);

        public static TimeSpan TimeSpan => SelectedTimeSpan ?? ToUTC.Subtract(FromUTC);
    }
}