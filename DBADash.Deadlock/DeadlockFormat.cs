using System;
using System.Globalization;

namespace DBADash.Deadlock
{
    /// <summary>
    /// Shared value formatting for node labels and tooltips.
    ///
    /// Invariant culture throughout, and durations are rendered without a decimal separator, so the
    /// text is identical in every culture.  That matters here because node sizing is derived from
    /// measured text - a culture dependent string would make layout (and its tests) non
    /// deterministic.
    /// </summary>
    internal static class DeadlockFormat
    {
        /// <summary>Milliseconds under ten seconds, whole seconds above.</summary>
        public static string Duration(TimeSpan value) =>
            value.TotalMilliseconds < 10000
                ? string.Format(CultureInfo.InvariantCulture, "{0:0} ms", value.TotalMilliseconds)
                : string.Format(CultureInfo.InvariantCulture, "{0:0} s", value.TotalSeconds);

        public static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

        /// <summary>
        /// An identifier - a hobt id, an object id, a process id.  Written plainly, with no thousands
        /// separators: grouping is for quantities, where it helps the eye judge magnitude.  An id has
        /// no magnitude to judge, and the separators actively get in the way of the thing ids are for
        /// - matching one value against another, and pasting it into a query.
        /// </summary>
        public static string Identifier(long value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// A byte count in the largest unit that keeps the number short.  Used where space is tight -
        /// a long running transaction can burn megabytes of log, and the exact figure is a tooltip
        /// away.
        /// </summary>
        public static string Bytes(long value)
        {
            const long kilobyte = 1024;
            const long megabyte = kilobyte * 1024;
            const long gigabyte = megabyte * 1024;

            return value switch
            {
                < kilobyte => string.Format(CultureInfo.InvariantCulture, "{0:0} bytes", value),
                < megabyte => string.Format(CultureInfo.InvariantCulture, "{0:0.#} KB", value / (double)kilobyte),
                < gigabyte => string.Format(CultureInfo.InvariantCulture, "{0:0.#} MB", value / (double)megabyte),
                _ => string.Format(CultureInfo.InvariantCulture, "{0:0.#} GB", value / (double)gigabyte)
            };
        }

        public static string Number(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

        /// <summary>
        /// Collapses runs of whitespace onto one line and truncates, so a multi line statement can
        /// sit on a single tooltip row.
        /// </summary>
        public static string SingleLine(string value, int maxLength)
        {
            var collapsed = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return collapsed.Length <= maxLength ? collapsed : collapsed[..maxLength] + "...";
        }
    }
}
