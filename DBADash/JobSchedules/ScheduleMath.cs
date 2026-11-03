using System;
using System.Collections.Generic;

namespace DBADash.JobSchedules
{
    /// <summary>Arithmetic and wording the schedule classes share</summary>
    public static class ScheduleMath
    {
        public static long Gcd(long a, long b)
        {
            while (b != 0) (a, b) = (b, a % b);
            return Math.Abs(a);
        }

        /// <summary>The remainder, never negative: -5 mod 60 is 55</summary>
        public static long Mod(long value, long modulus) => ((value % modulus) + modulus) % modulus;

        public static int Mod(int value, int modulus) => ((value % modulus) + modulus) % modulus;

        /// <summary>1h 5m 30s, 45s, or 0s</summary>
        public static string Span(long seconds)
        {
            seconds = Math.Abs(seconds);
            var parts = new List<string>();
            if (seconds >= 3600) parts.Add($"{seconds / 3600}h");
            if (seconds % 3600 >= 60) parts.Add($"{seconds % 3600 / 60}m");
            if (seconds % 60 != 0 || parts.Count == 0) parts.Add($"{seconds % 60}s");
            return string.Join(" ", parts);
        }

        /// <summary>A move: +1h 5m 30s, -45s, or 0</summary>
        public static string Shift(long seconds) => seconds == 0 ? "0" : (seconds < 0 ? "-" : "+") + Span(seconds);
    }
}
