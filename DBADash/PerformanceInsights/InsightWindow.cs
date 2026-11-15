using System;

namespace DBADash.PerformanceInsights
{
    /// <summary>
    /// The whole hours the Performance tab's insights look at for a date range, and whether they're compared with the baseline for the time.
    ///
    /// <para>The figures come from the hourly rollups, so the range is widened to whole hours - from the start of its first hour to the end of
    /// its last.  The baseline is the same hours 1 to <see cref="BaselineWeeks"/> weeks earlier: shifting by whole weeks keeps both the time of day
    /// and the day of the week, so a quiet Sunday is compared with quiet Sundays.  A range of more than a week isn't compared, as its baseline
    /// weeks would overlap it and each other - only thresholds and what happened are reported for it.</para>
    /// </summary>
    public sealed class InsightWindow
    {
        /// <summary>Longest window compared with the weeks before it - longer would overlap the week before</summary>
        public const int MaxBaselineHours = 168;

        /// <summary>Longest window the performance figures are read for (31 days).  Longer ranges get the other insights only</summary>
        public const int MaxHours = 744;

        public const int DefaultBaselineWeeks = 4;

        /// <summary>The date range asked for</summary>
        public DateTime FromUtc { get; init; }
        public DateTime ToUtc { get; init; }

        /// <summary>Start of the first whole hour</summary>
        public DateTime StartUtc { get; init; }
        /// <summary>End of the last whole hour</summary>
        public DateTime EndUtc { get; init; }

        public int Hours => (int)Math.Round((EndUtc - StartUtc).TotalHours);

        /// <summary>Weeks compared with.  0 when the window isn't compared (longer than a week)</summary>
        public int BaselineWeeks { get; init; }

        public bool IsCompared => BaselineWeeks > 0;

        /// <summary>Too long to read the performance figures for - see <see cref="MaxHours"/></summary>
        public bool IsTooLong => Hours > MaxHours;

        /// <summary>The range was widened to whole hours</summary>
        public bool IsRounded => StartUtc != FromUtc || EndUtc != ToUtc;

        public static InsightWindow For(DateTime fromUtc, DateTime toUtc, int baselineWeeks = DefaultBaselineWeeks)
        {
            if (toUtc <= fromUtc) toUtc = fromUtc.AddMinutes(1);
            var start = FloorHour(fromUtc);
            var end = CeilingHour(toUtc);
            var hours = (int)Math.Round((end - start).TotalHours);
            return new InsightWindow
            {
                FromUtc = fromUtc,
                ToUtc = toUtc,
                StartUtc = start,
                EndUtc = end,
                BaselineWeeks = hours <= MaxBaselineHours ? Math.Max(0, baselineWeeks) : 0
            };
        }

        public static DateTime FloorHour(DateTime d) => new(d.Year, d.Month, d.Day, d.Hour, 0, 0, DateTimeKind.Utc);

        public static DateTime CeilingHour(DateTime d)
        {
            var floor = FloorHour(d);
            return floor == DateTime.SpecifyKind(d, DateTimeKind.Utc) ? floor : floor.AddHours(1);
        }

        /// <summary>Same window - for caching the figures read for it</summary>
        public string Key => $"{StartUtc:yyyyMMddHH}-{EndUtc:yyyyMMddHH}-{BaselineWeeks}";
    }
}
