using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI
{
    internal static class DateHelper
    {
        /// <summary>
        /// List of date groups to use for charts
        /// </summary>
        public static readonly Dictionary<int, string> DateGroups = new() {
                {0,"None" },
                { 1, "1min" },
                { 2, "2min" },
                { 5, "5min" },
                { 10, "10min" },
                { 15, "15min" },
                { 30, "30min" },
                { 60, "1hr" },
                { 120, "2hrs" },
                { 240, "4hrs" },
                { 360, "6hrs" },
                { 720, "12hrs" },
                { 1440, "1 Day" },
                { 2880, "2 Days" },
                { 10880, "7 Days" },
                { 20160, "14 Days" }
            };

        /// <summary>
        /// Time zone of the application.  Default to OS time zone
        /// </summary>
        public static TimeZoneInfo AppTimeZone = TimeZoneInfo.Local;

        /// <summary>
        /// Return Utc offset for app time zone
        /// </summary>
        public static int UtcOffset => (int)Math.Ceiling(AppTimeZone.GetUtcOffset(DateTime.UtcNow).TotalMinutes);

        /// <summary>
        /// Add Date group menu.  None, 1min, 2min etc
        /// </summary>
        public static void AddDateGroups(ToolStripDropDownButton tsRoot, EventHandler click)
        {
            foreach (var dg in DateGroups)
            {
                var ts = new ToolStripMenuItem(dg.Value)
                {
                    Tag = dg.Key
                };
                ts.Click += click;
                tsRoot.DropDownItems.Add(ts);
            }
        }

        /// <summary>
        /// Convert DateTime values in DataTable from Utc format to App time zone
        /// </summary>
        /// <param name="dt">DataTable to convert</param>
        /// <param name="convertCols">Option to specify a list of columns to convert.  Default to all columns</param>
        /// <returns></returns>
        public static DataTable ConvertUTCToAppTimeZone(ref DataTable dt, List<string> convertCols = null)
        {
            List<int> convertColsIdx = new();
            if (convertCols == null || convertCols.Count == 0)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.DataType == typeof(DateTime) || col.DataType == typeof(DateTimeOffset))
                    {
                        convertColsIdx.Add(col.Ordinal);
                    }
                }
            }
            else
            {
                foreach (string col in convertCols)
                {
                    convertColsIdx.Add(dt.Columns[col].Ordinal);
                }
            }
            if (convertColsIdx.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    foreach (var colIdx in convertColsIdx)
                    {
                        if (row[colIdx] != DBNull.Value)
                        {
                            var colType = dt.Columns[colIdx].DataType;
                            if (colType == typeof(DateTime))
                            {
                                row[colIdx] = ((DateTime)row[colIdx]).ToAppTimeZone();
                            }
                            else if (colType == typeof(DateTimeOffset))
                            {
                                row[colIdx] = ((DateTimeOffset)row[colIdx]).ToAppTimeZone();
                            }
                        }
                    }
                }
            }
            return dt;
        }

        /// <summary>
        /// Get date grouping in minutes that will provide the optimal number of points for the chart based on MaxPoints
        /// </summary>
        /// <param name="Mins">Duration in minutes</param>
        /// <param name="MaxPoints">Max points for chart</param>
        /// <returns>Int value for the date grouping in minutes</returns>
        public static int DateGrouping(int Mins, int MaxPoints)
        {
            int lastMins = 0;

            foreach (var mins in DateGroups.OrderBy(k => k.Key)
                .Select(k => k.Key)
                .ToList())
            {
                double div = mins == 0 ? 0.2 : mins; // Use a fractional value for 0 (None).  0.2 = Assume 5 points per min.
                if (Mins / div < MaxPoints)
                {
                    return mins;
                }
                lastMins = mins;
            }
            return lastMins; // We are over MaxPoints but have ran out of date groups so return the last date group.
        }

        public static string DateGroupString(int mins)
        {
            return DateGroups.First(k => k.Key == mins).Value;
        }

        /// <summary>
        /// Extension method to convert DateTime value from Utc time zone to app time zone
        /// </summary>
        public static DateTime ToAppTimeZone(this DateTime value)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(value, AppTimeZone);
        }

        /// <summary>
        /// Extension method to convert DateTime value from app time zone to Utc
        /// </summary>
        public static DateTime AppTimeZoneToUtc(this DateTime value)
        {
            return TimeZoneInfo.ConvertTimeToUtc(value, AppTimeZone);
        }

        public static DateTimeOffset ToAppTimeZone(this DateTimeOffset value)
        {
            return TimeZoneInfo.ConvertTime(value, AppTimeZone);
        }

        /// <summary>
        /// Returns current DateTime in time zone of app
        /// </summary>
        public static DateTime AppNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppTimeZone);
    }
}