using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DBADashGUI.Performance
{
    public static class DateRange
    {
        private static int mins = 60;
        private static DateTime customFrom = DateTime.MinValue;
        private static DateTime customTo = DateTime.MinValue;
        private static List<int> timeOfDay = new();
        private static List<int> dayOfWeek = new();

        public static bool CurrentDateRangeSupportsTimeOfDayFilter => DurationMins >= 1440;

        public static bool CurrentDateRangeSupportsDayOfWeekFilter => DurationMins >= 10080;

        public static string DateFormatString => DateRange.DurationMins < 1440 ? DBADashUser.TimeFormatString : DBADashUser.DateTimeFormatString;

        public static void SetMins(int minutes)
        {
            mins = minutes;
            customFrom = DateTime.MinValue;
            customTo = DateTime.MinValue;
            ResetIfNotSupported();
        }

        public static void SetCustom(DateTime fromUTC, DateTime toUTC)
        {
            customFrom = fromUTC;
            customTo = toUTC;
            mins = -1;
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
                var utc = DateTime.UtcNow.AddMinutes(-mins);
                if (mins < 0)
                {
                    return customFrom;
                }
                else if (mins > 120)
                {
                    return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0);
                }
                else
                {
                    return utc;
                }
            }
        }

        public static DateTime ToUTC => mins < 0 || customTo == DateTime.MaxValue ? customTo : DateTime.UtcNow;

        public static int DurationMins => mins > 0 ? mins : Convert.ToInt32(TimeSpan.TotalMinutes);

        public static TimeSpan TimeSpan => ToUTC.Subtract(FromUTC);
    }
}