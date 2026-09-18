using System;
using System.Collections.Generic;
using System.Globalization;

namespace DBADash.QueryPlan
{
    /// <summary>
    /// Shared value formatting for node captions, tooltips and property lists.
    ///
    /// Invariant culture throughout.  That matters here because node sizing is derived from measured
    /// text - a culture dependent string would make layout, and its tests, non deterministic - and
    /// because a plan pasted into a ticket should read the same whoever pasted it.
    /// </summary>
    internal static class PlanFormat
    {
        /// <summary>
        /// A row count.  Estimates are fractional and a fractional row is a real thing to see on a
        /// plan - an estimate of 0.3 rows is the optimiser saying it expects almost none - so small
        /// values keep a decimal place rather than rounding to a misleading 0 or 1.
        /// </summary>
        public static string Rows(double value)
        {
            if (value < 0) return "0";
            if (value == 0) return "0";
            if (value < 10) return value.ToString("0.###", CultureInfo.InvariantCulture);

            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string Rows(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

        /// <summary>
        /// A large count shortened to fit a node caption: 1.2M rather than 1,234,567.  The exact
        /// figure is in the tooltip; on the node it has to lose to the operator name.
        /// </summary>
        public static string CompactCount(double value)
        {
            if (value < 0) return "0";

            return value switch
            {
                < 1000 => Rows(value),
                < 1_000_000 => (value / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "K",
                < 1_000_000_000 => (value / 1_000_000).ToString("0.#", CultureInfo.InvariantCulture) + "M",
                _ => (value / 1_000_000_000).ToString("0.#", CultureInfo.InvariantCulture) + "B"
            };
        }

        /// <summary>
        /// A share of the total as a percentage.  Anything that rounds to zero but is not zero shows
        /// as &lt;1% rather than 0%, because "this operator costs nothing" and "this operator costs
        /// less than a percent" are different claims and only one of them is true.
        /// </summary>
        public static string Percent(double fraction)
        {
            var percent = fraction * 100;

            if (percent > 0 && percent < 1) return "<1%";
            if (percent >= 10) return percent.ToString("0", CultureInfo.InvariantCulture) + "%";

            return percent.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>
        /// An optimiser cost.  Unitless, and printed to three significant figures, since comparing
        /// costs is the only thing they are good for.
        /// </summary>
        public static string Cost(double value) =>
            value >= 100
                ? value.ToString("N0", CultureInfo.InvariantCulture)
                : value.ToString("0.####", CultureInfo.InvariantCulture);

        /// <summary>Milliseconds under ten seconds, whole seconds above.</summary>
        public static string Duration(long milliseconds) =>
            milliseconds < 10000
                ? milliseconds.ToString("N0", CultureInfo.InvariantCulture) + " ms"
                : (milliseconds / 1000.0).ToString("N1", CultureInfo.InvariantCulture) + " s";

        /// <summary>
        /// A duration short enough for a node caption, to about three significant figures: 807 ms,
        /// 1.82 s, 15.3 s, 3m 46s.  The exact figure is in the tooltip; on the node, "1,154 ms"
        /// spends two characters more than "1.15 s" to say nothing the reader will act on.
        /// </summary>
        public static string ShortDuration(long milliseconds)
        {
            var culture = CultureInfo.InvariantCulture;

            switch (milliseconds)
            {
                case < 0:
                    return "0 ms";
                case < 1000:
                    return milliseconds.ToString(culture) + " ms";
                case < 9995:
                    return (milliseconds / 1000.0).ToString("0.00", culture) + " s";
                case < 60_000:
                    return (milliseconds / 1000.0).ToString("0.0", culture) + " s";
            }

            // Rounded to the unit shown before it is split, so 3m 45.9s says 3m 46s and 59m 59.6s
            // moves up to the hour rather than saying 59m 60s.
            var seconds = (long)Math.Round(milliseconds / 1000.0, MidpointRounding.AwayFromZero);
            if (seconds < 3600)
            {
                return (seconds / 60).ToString(culture) + "m " + (seconds % 60).ToString("00", culture) + "s";
            }

            var minutes = (long)Math.Round(milliseconds / 60_000.0, MidpointRounding.AwayFromZero);
            return (minutes / 60).ToString(culture) + "h " + (minutes % 60).ToString("00", culture) + "m";
        }

        /// <summary>
        /// A byte count in the largest unit that keeps the number short - an arrow label, where
        /// 1.2 GB is readable and 1,288,490,188 bytes is not.
        /// </summary>
        public static string Bytes(double value)
        {
            const double kilobyte = 1024;
            const double megabyte = kilobyte * 1024;
            const double gigabyte = megabyte * 1024;
            const double terabyte = gigabyte * 1024;

            return value switch
            {
                <= 0 => "0 B",
                < kilobyte => value.ToString("0", CultureInfo.InvariantCulture) + " B",
                < megabyte => (value / kilobyte).ToString("0.#", CultureInfo.InvariantCulture) + " KB",
                < gigabyte => (value / megabyte).ToString("0.#", CultureInfo.InvariantCulture) + " MB",
                < terabyte => (value / gigabyte).ToString("0.#", CultureInfo.InvariantCulture) + " GB",
                _ => (value / terabyte).ToString("0.#", CultureInfo.InvariantCulture) + " TB"
            };
        }

        /// <summary>A kilobyte count in the largest unit that keeps the number short.</summary>
        public static string Kilobytes(long value)
        {
            const long megabyte = 1024;
            const long gigabyte = megabyte * 1024;

            return value switch
            {
                < megabyte => value.ToString("N0", CultureInfo.InvariantCulture) + " KB",
                < gigabyte => (value / (double)megabyte).ToString("0.#", CultureInfo.InvariantCulture) + " MB",
                _ => (value / (double)gigabyte).ToString("0.##", CultureInfo.InvariantCulture) + " GB"
            };
        }

        /// <summary>
        /// An estimate-to-actual ratio, as the multiple a reader would say out loud: "12x more than
        /// estimated", or "50x fewer".  A bare ratio of 0.02 takes a moment to interpret and the
        /// moment is spent every time.
        /// </summary>
        public static string EstimateRatio(double ratio)
        {
            // Zero rows is not a multiple of anything - "0x" reads as a typo - and an operator that
            // returned nothing is often the interesting fact itself: a cancelled query, or a filter
            // that eliminated everything the optimiser expected to keep.
            if (ratio <= 0) return "no rows returned";

            // Within a tenth either way the estimate was right, and "1x more" reads as a problem
            // when it is the opposite of one.
            if (ratio is >= 0.9 and <= 1.1) return "as estimated";

            if (ratio >= 1)
            {
                return ratio.ToString(ratio >= 10 ? "0" : "0.#", CultureInfo.InvariantCulture) + "x more";
            }

            var inverse = 1 / ratio;
            return inverse.ToString(inverse >= 10 ? "0" : "0.#", CultureInfo.InvariantCulture) + "x fewer";
        }

        /// <summary>
        /// Collapses runs of whitespace onto one line and truncates, so a multi line statement can
        /// sit on a single caption or tooltip row.
        /// </summary>
        public static string SingleLine(string value, int maxLength)
        {
            var collapsed = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return collapsed.Length <= maxLength ? collapsed : collapsed[..maxLength] + "...";
        }

        /// <summary>
        /// A long plan value laid out for reading rather than scanning: each item of a list - defined
        /// values, output columns, seek keys - on a line of its own, and each AND and OR of a
        /// predicate starting a new line.
        ///
        /// Only at the top level.  A comma inside a function call, a bracketed name or a string literal
        /// is part of the item it is in, so nesting is tracked rather than splitting on every comma.
        /// </summary>
        public static string ForReading(string value)
        {
            var lines = new List<string>();
            var line = new System.Text.StringBuilder();
            var depth = 0;
            var inBrackets = false;
            var inQuotes = false;

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];

                if (inQuotes)
                {
                    if (c == '\'') inQuotes = false;
                }
                else if (inBrackets)
                {
                    if (c == ']') inBrackets = false;
                }
                else if (c == '\'') inQuotes = true;
                else if (c == '[') inBrackets = true;
                else if (c == '(') depth++;
                else if (c == ')') depth = Math.Max(0, depth - 1);
                else if (depth == 0)
                {
                    if (c == ',')
                    {
                        Flush();
                        continue;
                    }

                    if (c == ' ' && (StartsWord(value, i + 1, "AND") || StartsWord(value, i + 1, "OR")))
                    {
                        Flush();
                        continue;
                    }
                }

                line.Append(c);
            }

            Flush();
            return string.Join(Environment.NewLine, lines);

            void Flush()
            {
                var text = line.ToString().Trim();
                if (text.Length > 0) lines.Add(text);
                line.Clear();
            }
        }

        /// <summary>True when <paramref name="word"/> starts at <paramref name="index"/> and is followed by a space.</summary>
        private static bool StartsWord(string value, int index, string word) =>
            index + word.Length < value.Length &&
            string.CompareOrdinal(value, index, word, 0, word.Length) == 0 &&
            value[index + word.Length] == ' ';

        /// <summary>
        /// A boolean as showplan means it: present and true is worth saying, false usually is not.
        /// </summary>
        public static string Boolean(bool value) => value ? "True" : "False";
    }
}
