using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DBADash
{
    /// <summary>
    /// Pure parsing and day-token utilities extracted from the UI CronExpressionBuilder for testability.
    /// This class provides a small, focused translator between Quartz-style 6-field cron expressions
    /// and a simple <see cref="ParsedCronState"/> model used by the UI and tests.
    /// </summary>
    public static class CronParser
    {
        /// <summary>
        /// Mode of the cron frequency as interpreted by the parser/builder.
        /// </summary>
        public enum FrequencyMode
        {
            None = 0,
            EveryNSeconds = 1,
            EveryNMinutes = 2,
            EveryNHours = 3,
            Daily = 4,
            Weekly = 5,
            Custom = 6,
            IntegerSeconds = 7,
            Default = 8
        }

        /// <summary>
        /// Structured representation of a parsed cron expression tailored to the builder UI.
        /// </summary>
        public class ParsedCronState
        {
            /// <summary>Interpreted frequency mode.</summary>
            public FrequencyMode Mode { get; set; }
            /// <summary>Interval used for step expressions (e.g. every N minutes).</summary>
            public int Interval { get; set; }
            /// <summary>Base seconds value for step or fixed-time expressions.</summary>
            public int BaseSecond { get; set; }
            /// <summary>Base minutes value for step or fixed-time expressions.</summary>
            public int BaseMinute { get; set; }
            /// <summary>Base hours value for step or fixed-time expressions.</summary>
            public int BaseHour { get; set; }
            /// <summary>Normalized day tokens (3-letter names) when applicable.</summary>
            public string[] SelectedDays { get; set; }
            /// <summary>Original custom expression when mode is Custom.</summary>
            public string CustomExpression { get; set; }
        }

        // Helper: canonical day names used in cron expressions
        public static string[] DayNames() => new[] { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };

        /// <summary>
        /// Map token (name prefix or number) to index 0..6 (SUN=0). Returns -1 if unrecognized.
        /// </summary>
        /// <param name="token">Day token (name prefix or numeric string 1..7).</param>
        /// <returns>Index 0..6 for recognized tokens, or -1 if unrecognized.</returns>
        private static int NameToIndex(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return -1;
            var s = token.Trim().ToUpperInvariant();
            if (int.TryParse(s, out int n) && n >= 1 && n <= 7) return n - 1;
            if (s.StartsWith("SUN")) return 0;
            if (s.StartsWith("MON")) return 1;
            if (s.StartsWith("TUE")) return 2;
            if (s.StartsWith("WED")) return 3;
            if (s.StartsWith("THU")) return 4;
            if (s.StartsWith("FRI")) return 5;
            if (s.StartsWith("SAT")) return 6;
            return -1;
        }

        /// <summary>
        /// Normalize various day token formats to canonical 3-letter tokens used by <see cref="DayNames"/>.
        /// </summary>
        /// <remarks>
        /// Accepts SUN, SUNDAY, Sun, MON, Monday, or numeric 1-7 (1=SUN).
        /// Also accepts ranges like MON-FRI (wrap-around like FRI-MON supported) and expands them to individual tokens.
        /// </remarks>
        /// <param name="tokens">Input tokens to normalize.</param>
        /// <returns>Array of canonical 3-letter tokens or <c>null</c> if any token is unrecognizable.</returns>
        public static string[] NormalizeDayTokens(string[] tokens)
        {
            if (tokens == null) return null;
            var outNames = new System.Collections.Generic.List<string>();
            foreach (var t in tokens)
            {
                if (string.IsNullOrWhiteSpace(t)) return null;
                var s = t.Trim().ToUpperInvariant();
                // Range form: START-END
                if (s.Contains('-'))
                {
                    var parts = s.Split('-', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2) return null;
                    var startIdx = NameToIndex(parts[0]);
                    var endIdx = NameToIndex(parts[1]);
                    if (startIdx < 0 || endIdx < 0) return null;
                    // expand range including wrap-around
                    int idx = startIdx;
                    while (true)
                    {
                        outNames.Add(DayNames()[idx]);
                        if (idx == endIdx) break;
                        idx = (idx + 1) % 7;
                    }
                    continue;
                }

                // numeric 1-7 -> map 1=SUN
                if (int.TryParse(s, out int n) && n >= 1 && n <= 7)
                {
                    outNames.Add(DayNames()[n - 1]);
                    continue;
                }
                // normalize common name prefixes
                if (s.StartsWith("SUN")) { outNames.Add("SUN"); continue; }
                if (s.StartsWith("MON")) { outNames.Add("MON"); continue; }
                if (s.StartsWith("TUE")) { outNames.Add("TUE"); continue; }
                if (s.StartsWith("WED")) { outNames.Add("WED"); continue; }
                if (s.StartsWith("THU")) { outNames.Add("THU"); continue; }
                if (s.StartsWith("FRI")) { outNames.Add("FRI"); continue; }
                if (s.StartsWith("SAT")) { outNames.Add("SAT"); continue; }
                return null;
            }
            return outNames.ToArray();
        }

        /// <summary>
        /// Compress an expanded list of day tokens (e.g. [MON,TUE,WED,THU,FRI]) into simplified tokens like [MON-FRI].
        /// </summary>
        /// <remarks>Preserves order and handles wrap-around ranges; returns ["*"] for all-days.</remarks>
        /// <param name="expanded">Expanded list of canonical day tokens.</param>
        /// <returns>Compressed tokens (ranges or single-day names) or an empty array for none.</returns>
        public static string[] CompressDayTokens(string[] expanded)
        {
            if (expanded == null || expanded.Length == 0) return Array.Empty<string>();
            var names = DayNames();
            var present = new bool[7];
            foreach (var d in expanded)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (string.Equals(names[i], d, StringComparison.OrdinalIgnoreCase))
                    {
                        present[i] = true;
                        break;
                    }
                }
            }
            var indices = Enumerable.Range(0, 7).Where(i => present[i]).ToList();
            if (indices.Count == 0) return Array.Empty<string>();
            if (indices.Count == 7) return new[] { "*" }; // all days expressed as '*'

            // Build linear runs
            var runs = new System.Collections.Generic.List<System.Collections.Generic.List<int>>();
            var currentRun = new System.Collections.Generic.List<int> { indices[0] };
            for (int k = 1; k < indices.Count; k++)
            {
                if (indices[k] == currentRun.Last() + 1)
                {
                    currentRun.Add(indices[k]);
                }
                else
                {
                    runs.Add(currentRun);
                    currentRun = new System.Collections.Generic.List<int> { indices[k] };
                }
            }
            runs.Add(currentRun);

            // If first run starts at 0 and last run ends at 6, merge for wrap-around
            if (runs.Count > 1 && runs.First().First() == 0 && runs.Last().Last() == 6)
            {
                var merged = new System.Collections.Generic.List<int>();
                merged.AddRange(runs.Last());
                merged.AddRange(runs.First());
                runs[0] = merged;
                runs.RemoveAt(runs.Count - 1);
            }

            var outList = new System.Collections.Generic.List<string>();
            foreach (var run in runs)
            {
                if (run.Count == 1)
                {
                    outList.Add(names[run[0]]);
                }
                else
                {
                    outList.Add(names[run.First()] + "-" + names[run.Last()]);
                }
            }
            return outList.ToArray();
        }

        /// <summary>
        /// Parse a Quartz-style 6-field cron expression into a <see cref="ParsedCronState"/>.
        /// </summary>
        /// <remarks>
        /// Supported shapes include step expressions (S/N, M/N, H/N), daily and weekly forms used by the builder.
        /// Unsupported or malformed expressions return <c>false</c> so the UI can fallback to Custom.
        /// Intervals of zero or less are rejected.
        /// </remarks>
        /// <param name="cron">Cron expression to parse (6 fields).</param>
        /// <param name="state">Output parsed state when parsing succeeds.</param>
        /// <returns>True if parsing succeeded and state is populated; otherwise false.</returns>
        public static bool TryParseCronState(string cron, out ParsedCronState state)
        {
            state = null;
            if (string.IsNullOrWhiteSpace(cron)) return false;
            var parts = cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6) return false;

            // small helper to parse and validate numeric cron fields
            static bool TryParseField(string token, int min, int max, out int value)
            {
                value = 0;
                if (!int.TryParse(token, out value)) return false;
                return value >= min && value <= max;
            }

            // Every N seconds: look for parts[0] like 'S/N'
            var secondStep = Regex.Match(parts[0], @"^(\d+)/(\d+)$");
            if (secondStep.Success)
            {
                // validate base seconds within Quartz range 0-59
                if (!TryParseField(secondStep.Groups[1].Value, 0, 59, out int baseSec) || !int.TryParse(secondStep.Groups[2].Value, out int interval))
                {
                    return false;
                }
                // reject non-positive intervals
                if (interval <= 0) return false;
                // No-day form: S/N * * * * ?
                if (parts[1] == "*" && parts[2] == "*" && parts[3] == "*" && parts[4] == "*" && parts[5] == "?")
                {
                    state = new ParsedCronState
                    {
                        Mode = FrequencyMode.EveryNSeconds,
                        Interval = interval,
                        BaseSecond = baseSec
                    };
                    return true;
                }
                // Day-specified: S/N * * ? * MON
                if (parts[1] == "*" && parts[2] == "*" && parts[3] == "?" && parts[4] == "*")
                {
                    var selectedDays = parts[5].Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var normalized = NormalizeDayTokens(selectedDays);
                    if (normalized == null) return false;
                    state = new ParsedCronState
                    {
                        Mode = FrequencyMode.EveryNSeconds,
                        Interval = interval,
                        BaseSecond = baseSec,
                        SelectedDays = normalized
                    };
                    return true;
                }
                return false;
            }

            // Every N minutes: support both "0/N" and "M/N" minute forms (e.g. "0/5" or "2/5")
            var minuteStepOnly = Regex.Match(parts[1], @"^0/(\d+)$");
            var minuteBaseStep = Regex.Match(parts[1], @"^(\d+)/(\d+)$");
            if (minuteStepOnly.Success || minuteBaseStep.Success)
            {
                bool parsed = false;
                int interval = 0;
                int baseMin = 0;
                if (minuteStepOnly.Success)
                {
                    parsed = int.TryParse(minuteStepOnly.Groups[1].Value, out interval);
                }
                else if (minuteBaseStep.Success)
                {
                    parsed = int.TryParse(minuteBaseStep.Groups[2].Value, out interval) && int.TryParse(minuteBaseStep.Groups[1].Value, out baseMin);
                }

                if (!parsed) return false;
                // reject non-positive intervals
                if (interval <= 0) return false;

                // No-day form: S M/N * * * ?
                if (parts[2] == "*" && parts[3] == "*" && parts[4] == "*" && parts[5] == "?")
                {
                    // require a numeric seconds field in range 0-59
                    if (!TryParseField(parts[0], 0, 59, out int secForMinutes)) return false;
                    // if a base minute was provided, validate its range 0-59
                    if (minuteBaseStep.Success && (baseMin < 0 || baseMin > 59)) return false;

                    state = new ParsedCronState
                    {
                        Mode = FrequencyMode.EveryNMinutes,
                        Interval = interval,
                        BaseMinute = baseMin,
                        BaseSecond = secForMinutes
                    };
                    return true;
                }

                // Day-specified form: S M/N * ? * MON,TUE
                if (parts[2] == "*" && parts[3] == "?" && parts[4] == "*")
                {
                    var selectedDays = parts[5].Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var normalized = NormalizeDayTokens(selectedDays);
                    if (normalized == null) return false;
                    // require a numeric seconds field in range 0-59
                    if (!TryParseField(parts[0], 0, 59, out int secForMinutes)) return false;
                    // if a base minute was provided, validate its range 0-59
                    if (minuteBaseStep.Success && (baseMin < 0 || baseMin > 59)) return false;

                    state = new ParsedCronState
                    {
                        Mode = FrequencyMode.EveryNMinutes,
                        Interval = interval,
                        BaseMinute = baseMin,
                        BaseSecond = secForMinutes,
                        SelectedDays = normalized
                    };
                    return true;
                }
                return false;
            }

            // Every N hours: accept forms like H/N in the hour field (e.g. 5/5)
            var hourMatch = Regex.Match(parts[2], @"^(\d+)/(\d+)$");
            if (hourMatch.Success)
            {
                if (!int.TryParse(hourMatch.Groups[1].Value, out int hBase) || !int.TryParse(hourMatch.Groups[2].Value, out int interval)) return false;
                // validate base hour range 0-23
                if (hBase < 0 || hBase > 23) return false;
                // reject non-positive intervals
                if (interval <= 0) return false;
                // no-day form: seconds from parts[0], minute from parts[1], hours as H/N
                if (parts[3] == "*" && parts[4] == "*" && parts[5] == "?")
                {
                    // require numeric minute and second fields within Quartz ranges
                    if (!TryParseField(parts[1], 0, 59, out int m)) return false;
                    if (!TryParseField(parts[0], 0, 59, out int secForHours)) return false;

                    state = new ParsedCronState
                    {
                        Mode = FrequencyMode.EveryNHours,
                        Interval = interval,
                        BaseHour = hBase,
                        BaseMinute = m,
                        BaseSecond = secForHours
                    };
                    return true;
                }

                // day-specified form: S M H/N ? * MON,TUE
                if (parts[3] == "?" && parts[4] == "*")
                {
                    var selectedDays = parts[5].Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var normalized = NormalizeDayTokens(selectedDays);
                    if (normalized == null) return false;
                    // require numeric minute and second fields within Quartz ranges
                    if (!TryParseField(parts[1], 0, 59, out int m2)) return false;
                    if (!TryParseField(parts[0], 0, 59, out int secForHours2)) return false;

                    state = new ParsedCronState
                    {
                        Mode = FrequencyMode.EveryNHours,
                        Interval = interval,
                        BaseHour = hBase,
                        BaseMinute = m2,
                        BaseSecond = secForHours2,
                        SelectedDays = normalized
                    };
                    return true;
                }
                return false;
            }

            // Daily: S M H * * ?  (seconds, minutes, hours)
            // require numeric seconds/minutes/hours and validate Quartz ranges (sec/min 0-59, hour 0-23)
            if (parts[3] == "*" && parts[4] == "*" && parts[5] == "?"
                && TryParseField(parts[0], 0, 59, out int s)
                && TryParseField(parts[1], 0, 59, out int min)
                && TryParseField(parts[2], 0, 23, out int hr))
            {
                state = new ParsedCronState
                {
                    Mode = FrequencyMode.Daily,
                    BaseHour = hr,
                    BaseMinute = min,
                    BaseSecond = s
                };
                return true;
            }

            // Weekly: S M H ? * DAY,DAY,...
            // require numeric seconds/minutes/hours and validate Quartz ranges (sec/min 0-59, hour 0-23)
            if (parts[3] == "?" && parts[4] == "*"
                && TryParseField(parts[1], 0, 59, out int wMin)
                && TryParseField(parts[2], 0, 23, out int wHr)
                && TryParseField(parts[0], 0, 59, out int wSec))
            {
                var selectedDays = parts[5].Split(',', StringSplitOptions.RemoveEmptyEntries);
                var normalized = NormalizeDayTokens(selectedDays);
                if (normalized == null) return false;
                state = new ParsedCronState
                {
                    Mode = FrequencyMode.Weekly,
                    BaseHour = wHr,
                    BaseMinute = wMin,
                    BaseSecond = wSec,
                    SelectedDays = normalized
                };
                return true;
            }

            return false;
        }
    }
}
