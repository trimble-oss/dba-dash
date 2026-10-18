#nullable enable
using System;

namespace DBADash.PerformanceAnalysis
{
    /// <summary>
    /// The period a performance analysis covers, and the baseline it is compared with.
    ///
    /// Capped at <see cref="MaxLength"/>.  A year of stats summarised into a payload says nothing a model
    /// can act on - the incident the reader is chasing is an afternoon in it - and the cap is also what
    /// keeps the baseline (the same window a week earlier) from overlapping the window itself.
    /// </summary>
    public sealed class PerformanceAnalysisWindow
    {
        public static readonly TimeSpan MaxLength = TimeSpan.FromDays(7);

        /// <summary>The baseline is the same window this much earlier - same time of day, same day of the week.</summary>
        public static readonly TimeSpan BaselineOffset = TimeSpan.FromDays(7);

        /// <summary>Roughly how many points a time series is given, whatever the length of the window.</summary>
        public const int TargetPoints = 48;

        /// <summary>
        /// Bucket sizes a series can be grouped by.  Past an hour they are whole hours, because the hourly
        /// rollups can be added into a bucket of a whole number of hours but not split into smaller ones.
        /// </summary>
        private static readonly int[] BucketOptions = [1, 2, 5, 10, 15, 30, 60, 120, 180, 240, 360, 720, 1440];

        public DateTime FromUtc { get; }
        public DateTime ToUtc { get; }

        /// <summary>True when the window asked for was longer than <see cref="MaxLength"/> and was cut to its end.</summary>
        public bool WasClamped { get; }

        private PerformanceAnalysisWindow(DateTime fromUtc, DateTime toUtc, bool wasClamped)
        {
            FromUtc = fromUtc;
            ToUtc = toUtc;
            WasClamped = wasClamped;
        }

        /// <summary>
        /// The window asked for, cut to the last <see cref="MaxLength"/> of it where it is longer.  The end
        /// is kept rather than the start: a reader looking at a long range is nearly always asking about
        /// what is happening now.
        /// </summary>
        public static PerformanceAnalysisWindow Create(DateTime fromUtc, DateTime toUtc)
        {
            if (toUtc <= fromUtc)
            {
                throw new ArgumentException("The window must end after it starts.", nameof(toUtc));
            }

            return toUtc - fromUtc > MaxLength
                ? new PerformanceAnalysisWindow(toUtc - MaxLength, toUtc, true)
                : new PerformanceAnalysisWindow(fromUtc, toUtc, false);
        }

        public TimeSpan Length => ToUtc - FromUtc;

        /// <summary>The size of the buckets the window's time series are grouped into.</summary>
        public int BucketMinutes => ChooseBucketMinutes(Length);

        /// <summary>
        /// The smallest bucket that keeps a series to about <see cref="TargetPoints"/>.  A day is 30-minute
        /// buckets, a week four-hour ones.
        /// </summary>
        public static int ChooseBucketMinutes(TimeSpan length)
        {
            var wanted = length.TotalMinutes / TargetPoints;
            foreach (var option in BucketOptions)
            {
                if (option >= wanted) return option;
            }

            return BucketOptions[^1];
        }

        /// <summary>
        /// The same window a week earlier, widened to whole hours.
        ///
        /// Widened because the baseline is read from the hourly rollups.  The raw tables are kept for days
        /// rather than weeks, so a week-old window at minute grain has often been purged already, and the
        /// rollups only hold whole hours - a 20-minute window inside an hour holds no hourly row at all.
        /// Every figure compared is a rate, so a slightly longer baseline still compares like with like.
        /// </summary>
        public (DateTime FromUtc, DateTime ToUtc) Baseline
        {
            get
            {
                var from = FloorHour(FromUtc - BaselineOffset);
                var to = CeilingHour(ToUtc - BaselineOffset);
                return (from, to == from ? from.AddHours(1) : to);
            }
        }

        private static DateTime FloorHour(DateTime value) =>
            new(value.Year, value.Month, value.Day, value.Hour, 0, 0, value.Kind);

        private static DateTime CeilingHour(DateTime value)
        {
            var floor = FloorHour(value);
            return floor == value ? floor : floor.AddHours(1);
        }

        /// <summary>A length as a reader would say it: "45 minutes", "6 hours", "2 days 4 hours".</summary>
        public static string Describe(TimeSpan length)
        {
            if (length.TotalMinutes < 120) return $"{length.TotalMinutes:0} minutes";
            if (length.TotalHours < 48) return $"{length.TotalHours:0.#} hours";

            var days = (int)length.TotalDays;
            var hours = length.Hours;
            return hours == 0 ? $"{days} days" : $"{days} days {hours} hours";
        }
    }
}
