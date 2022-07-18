using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.Performance
{
    public static class DateRange
    {

        private static int mins = 60;
        private static DateTime customFrom = DateTime.MinValue;
        private static DateTime customTo = DateTime.MinValue;
        private static List<int> timeOfDay = new();
        private static List<int> dayOfWeek = new();

        public static bool CurrentDateRangeSupportsTimeOfDayFilter
        {
            get
            {
                return DurationMins >= 1440;
            }
        }

        public static bool CurrentDateRangeSupportsDayOfWeekFilter
        {
            get
            {
                return DurationMins >= 10080;
            }
        }

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


        public static bool HasTimeOfDayFilter
        {
            get
            {
                return timeOfDay.Count > 0 && timeOfDay.Count < 24;
            }
        }

        public static bool HasDayOfWeekFilter
        {
            get
            {
                return dayOfWeek.Count > 0 && dayOfWeek.Count < 7;
            }
        }

        public static List<int> TimeOfDay
        {
            get
            {
                return timeOfDay;
            }
            set {
                if (value.Count > 24)
                {
                    throw new Exception("Invalid time of day filter. Expected 24 items or less");
                }
                if (value.Where(hr=> hr>23 || hr < 0).Any()){
                    throw new Exception("Invalid time of day filter. Expected values 0..23");
                }
                if (value.GroupBy(hr => hr).Where(grp => grp.Count() > 1).Any())
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
            get
            {
                return dayOfWeek;
            }
            set
            {
                if (value.Count > 7)
                {
                    throw new Exception("Invalid day of week filter.  Expected 7 items or less");
                }
                if (value.Where(dow => dow > 7 || dow < 1).Any())
                {
                    throw new Exception("Invalid days of week filter. Expected values 1..7");
                }
                if (value.GroupBy(dow => dow).Where(grp => grp.Count() > 1).Any())
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

        public static DateTime ToUTC
        {
            get
            {
                if (mins < 0 || customTo == DateTime.MaxValue)
                {
                    return customTo;
                }
                else
                {
                    return DateTime.UtcNow;
                }
            }
        }

        public static int DurationMins
        {
            get
            {
                return mins>0 ? mins : Convert.ToInt32(ToUTC.Subtract(FromUTC).TotalMinutes);
            }
        }

    }
}
