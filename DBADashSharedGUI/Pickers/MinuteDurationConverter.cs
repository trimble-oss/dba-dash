using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DBADashGUI.Pickers
{
    /// <summary>
    /// Renders and parses a duration stored as a number of minutes.  Used alongside
    /// <see cref="MinuteDurationEditor"/> so a PropertyGrid row shows "7 days 4 hrs 1 min"
    /// instead of a raw minute count, and still accepts typed input such as "7d 4h 1m",
    /// "90m" or a bare number of minutes.
    /// </summary>
    public class MinuteDurationConverter : TypeConverter
    {
        private static readonly Regex TokenRegex = new(
            @"(?<value>\d+(\.\d+)?)\s*(?<unit>days?|d|hours?|hrs?|h|minutes?|mins?|m)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return value == null ? string.Empty : FormatMinutes(Convert.ToDecimal(value, culture));
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string s)
            {
                var propertyType = context?.PropertyDescriptor?.PropertyType;
                var isNullable = propertyType != null && Nullable.GetUnderlyingType(propertyType) != null;
                var underlyingType = propertyType == null ? null : Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                // A blank value clears a nullable property (e.g. "rely on critical status only").
                if (string.IsNullOrWhiteSpace(s))
                {
                    if (isNullable) { return null; }
                    return underlyingType == typeof(int) ? 0 : (object)0m;
                }

                var minutes = ParseMinutes(s, culture);
                return underlyingType == typeof(int) ? (int)Math.Round(minutes, MidpointRounding.AwayFromZero) : (object)minutes;
            }

            return base.ConvertFrom(context, culture, value);
        }

        private static string FormatMinutes(decimal totalMinutes)
        {
            var total = (long)Math.Max(0, Math.Round(totalMinutes));
            var days = total / 1440;
            var hours = (total % 1440) / 60;
            var minutes = total % 60;

            var parts = new List<string>();
            if (days > 0) { parts.Add($"{days} {(days == 1 ? "day" : "days")}"); }
            if (hours > 0) { parts.Add($"{hours} {(hours == 1 ? "hr" : "hrs")}"); }
            if (minutes > 0 || parts.Count == 0) { parts.Add($"{minutes} {(minutes == 1 ? "min" : "mins")}"); }

            return string.Join(" ", parts);
        }

        private static decimal ParseMinutes(string text, CultureInfo culture)
        {
            var trimmed = text?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                return 0;
            }

            // A bare number is treated as minutes.
            if (decimal.TryParse(trimmed, NumberStyles.Number, culture, out var bareMinutes) ||
                decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out bareMinutes))
            {
                return bareMinutes;
            }

            decimal total = 0;
            foreach (Match match in TokenRegex.Matches(trimmed))
            {
                var number = decimal.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
                var unit = match.Groups["unit"].Value.ToLowerInvariant();
                total += unit[0] switch
                {
                    'd' => number * 1440m,
                    'h' => number * 60m,
                    _ => number
                };
            }

            // Reject input that isn't fully consumed by valid tokens (e.g. "1h abc"),
            // so typos surface as an error rather than being silently ignored.
            var leftover = TokenRegex.Replace(trimmed, string.Empty);
            if (!string.IsNullOrWhiteSpace(leftover))
            {
                throw new FormatException($"'{text}' is not a valid duration. Use e.g. '7d 4h 1m' or a number of minutes.");
            }

            return total;
        }
    }
}
