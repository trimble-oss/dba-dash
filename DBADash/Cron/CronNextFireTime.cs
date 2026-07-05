using System;
using Quartz;

namespace DBADash
{
    /// <summary>
    /// Computes the next occurrence of a Quartz cron expression on demand, rather than persisting
    /// a next-fire timestamp that would go stale the moment the job actually fires.
    /// </summary>
    public static class CronNextFireTime
    {
        public static DateTime? TryGetNextFireTimeUtc(string normalizedCron, DateTimeOffset afterUtc)
        {
            if (string.IsNullOrEmpty(normalizedCron)) return null;
            if (int.TryParse(normalizedCron, out var seconds))
            {
                // Integer-seconds interval schedule rather than a cron expression - there's no fixed
                // anchor to compute an exact next fire time from, so approximate as one interval from now
                // (same assumption CronParser.TryGetMaxIntervalMinutes makes for these schedules).
                return seconds > 0 ? afterUtc.AddSeconds(seconds).UtcDateTime : null;
            }
            try
            {
                var expr = new CronExpression(normalizedCron);
                return expr.GetNextValidTimeAfter(afterUtc)?.UtcDateTime;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
