using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.Performance
{
    public static class DateRange
    {

        private static int mins=60;
        private static DateTime customFrom = DateTime.MinValue;
        private static DateTime customTo = DateTime.MinValue;
        public static void SetMins(int minutes)
        {
            mins = minutes;
            customFrom = DateTime.MinValue;
            customTo = DateTime.MinValue;
        }
        public static void SetCustom(DateTime fromUTC, DateTime toUTC)
        {
            customFrom = fromUTC;
            customTo = toUTC;
            mins = -1;
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
                return Convert.ToInt32(ToUTC.Subtract(FromUTC).TotalMinutes);
            }
        }



    }
}
