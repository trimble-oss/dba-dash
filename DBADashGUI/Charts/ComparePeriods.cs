using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADashGUI.Charts
{
    /// <summary>
    /// The Compare To choices shared by the Metrics and Object Execution charts, and the arithmetic they both need.
    /// A choice is stored as its key, so a saved view keeps working: "None", "Previous", "Baseline", or an offset in minutes.
    /// </summary>
    public static class ComparePeriods
    {
        public const string None = "None";
        public const string Previous = "Previous";
        public const string Baseline = "Baseline";
        public const string Custom = "Custom";

        /// <summary>The weeks the median baseline is taken from</summary>
        public const int BaselineWeeks = 4;

        /// <summary>The menu items, in order, as (menu text, key).  Custom is not here as it needs a date picker, which only Object Execution offers.</summary>
        public static readonly (string Text, string Key)[] Options =
        {
            ("None", None),
            ("Previous Period", Previous),
            ("-24hrs offset", "1440"),
            ("-7 days offset", "10080"),
            ("-28 days offset (4 weeks)", "40320"),
            ("Median of last 4 weeks (same time)", Baseline),
        };

        /// <summary>
        /// The periods compared to, as (from, to) in UTC, for the period from <paramref name="from"/> to <paramref name="to"/>.
        /// Empty for None, or for an unrecognised key.  The median baseline is one period per week.
        /// </summary>
        public static List<(DateTime From, DateTime To)> Windows(string key, DateTime from, DateTime to)
        {
            var windows = new List<(DateTime From, DateTime To)>();
            switch (key)
            {
                case Previous:
                    windows.Add((from - (to - from), from));
                    break;
                case Baseline:
                    for (var week = 1; week <= BaselineWeeks; week++)
                    {
                        windows.Add((from.AddDays(-7 * week), to.AddDays(-7 * week)));
                    }
                    break;
                default:
                    if (int.TryParse(key, out var offsetMins) && offsetMins > 0)
                    {
                        windows.Add((from.AddMinutes(-offsetMins), to.AddMinutes(-offsetMins)));
                    }
                    break;
            }
            return windows;
        }

        /// <summary>The median of the values.  Used for the baseline, where it is less affected by a single unusual week than the average.</summary>
        public static double Median(List<double> values)
        {
            var sorted = values.OrderBy(v => v).ToArray();
            var mid = sorted.Length / 2;
            return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }
    }
}
