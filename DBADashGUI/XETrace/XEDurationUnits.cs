using DBADash.XE;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Helpers for entering and showing microsecond duration fields (duration, cpu_time) as a number + unit, so users
    /// don't have to type or read raw microseconds (e.g. "10 sec" instead of 10000000, which Profiler-trained users
    /// also tend to misread as milliseconds).  The stored filter value is always whole microseconds - the unit XE
    /// expects in the DDL - so only the entry/display is unit-aware.
    /// </summary>
    internal static class XEDurationUnits
    {
        public sealed record Unit(string Label, decimal Micros);

        /// <summary>Selectable units, smallest first (index 0 = µs).</summary>
        public static readonly Unit[] Units =
        {
            new("µs", 1m),
            new("ms", 1_000m),
            new("sec", 1_000_000m),
            new("min", 60_000_000m),
        };

        /// <summary>Default entry unit - milliseconds, matching the Profiler duration column users are used to.</summary>
        public static Unit DefaultUnit => Units.First(u => u.Label == "ms");

        /// <summary>A fresh bindable copy of <see cref="Units"/> (each ComboBox needs its own list, same instances).</summary>
        public static List<Unit> BindingList() => new(Units);

        public static int IndexOf(Unit unit) => Array.FindIndex(Units, u => ReferenceEquals(u, unit));

        private static readonly HashSet<string> MicrosecondFields =
            new(StringComparer.OrdinalIgnoreCase) { "duration", "cpu_time" };

        public static bool IsDurationField(string fieldName, bool isNumeric) =>
            isNumeric && MicrosecondFields.Contains(fieldName ?? string.Empty);

        public static bool IsDurationField(XEFieldInfo f) => f != null && IsDurationField(f.Name, f.IsNumeric);

        public static bool IsDurationField(XEFilter f) => f != null && IsDurationField(f.Field, f.IsNumeric);

        /// <summary>Converts a typed magnitude + unit to a whole-microseconds string, or reports why it can't.</summary>
        public static bool TryToMicroseconds(string magnitudeText, Unit unit, out string microsText, out string error)
        {
            microsText = null;
            error = string.Empty;
            unit ??= DefaultUnit;

            if (!decimal.TryParse(magnitudeText?.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var magnitude)
                || magnitude < 0)
            {
                error = "Enter a non-negative number for the duration value";
                return false;
            }

            var micros = Math.Round(magnitude * unit.Micros, MidpointRounding.AwayFromZero);
            if (micros > long.MaxValue)
            {
                error = "Duration value is too large";
                return false;
            }

            microsText = ((long)micros).ToString(CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>Decomposes stored microseconds into the friendliest whole unit, for pre-filling an editor.</summary>
        public static (decimal Value, Unit Unit) Decompose(long micros)
        {
            foreach (var unit in Units.OrderByDescending(u => u.Micros))
            {
                if (micros % unit.Micros == 0m) return (micros / unit.Micros, unit);
            }
            return (micros, Units[0]); // µs always divides evenly; this is just a safety net
        }

        /// <summary>Display text for a stored microseconds value: friendliest whole unit, with raw µs for precision.</summary>
        public static string Humanize(string microsText)
        {
            if (!long.TryParse(microsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var micros) || micros < 0)
            {
                return microsText;
            }

            var (value, unit) = Decompose(micros);
            var scaled = value.ToString("0", CultureInfo.CurrentCulture);
            return unit.Micros == 1m
                ? $"{scaled} {unit.Label}"
                : $"{scaled} {unit.Label} ({micros.ToString("N0", CultureInfo.CurrentCulture)} µs)";
        }
    }
}
