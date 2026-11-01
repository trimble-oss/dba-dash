using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DBADashAI.Mcp;

namespace DBADashAI.Services
{
    /// <summary>
    /// The four drill-downs behind a performance analysis - root blockers, queries waiting on a wait type, long running
    /// queries and open transactions, and one deadlock signature's occurrences - shared by the AI service's analysis,
    /// which lets its model ask for them mid-answer, and the MCP server, which offers them as tools.  What each reads,
    /// what it is called with and how its result reads are here; how it is asked for is each caller's own.
    /// </summary>
    public static partial class PerformanceLookups
    {
        public const string Blocking = "blocking";
        public const string WaitingQueries = "waiting_queries";
        public const string LongRunning = "long_running";
        public const string Deadlock = "deadlock";

        public static readonly IReadOnlyList<string> All = [Blocking, WaitingQueries, LongRunning, Deadlock];

        public static string Procedure(string lookup) => lookup switch
        {
            Blocking => "AI.PerformanceInvestigationBlocking_Get",
            WaitingQueries => "AI.PerformanceInvestigationWaits_Get",
            LongRunning => "AI.PerformanceInvestigationLongRunning_Get",
            Deadlock => "AI.PerformanceInvestigationDeadlock_Get",
            _ => throw new ArgumentOutOfRangeException(nameof(lookup), lookup, "Not a performance lookup.")
        };

        /// <summary>The procedure's parameters: always the instance and window, plus what the lookup needs.</summary>
        public static List<(string Name, object? Value)> Parameters(
            string lookup, int instanceId, DateTime fromUtc, DateTime toUtc,
            string? waitType = null, bool tempDBOnly = false, string? signature = null)
        {
            var parameters = new List<(string, object?)>
            {
                ("@InstanceID", instanceId),
                ("@FromDate", fromUtc),
                ("@ToDate", toUtc)
            };

            if (lookup == WaitingQueries)
            {
                parameters.Add(("@WaitType", waitType));
                parameters.Add(("@TempDBOnly", tempDBOnly));
            }
            else if (lookup == Deadlock)
            {
                parameters.Add(("@Signature", signature));
            }

            return parameters;
        }

        [GeneratedRegex(@"^[A-Za-z0-9_%]{1,60}$")]
        private static partial Regex WaitTypePattern();

        [GeneratedRegex(@"^0x[0-9A-Fa-f]{16}$")]
        private static partial Regex SignaturePattern();

        /// <summary>A wait type, or a LIKE pattern of one such as PAGELATCH%.</summary>
        public static bool IsWaitType(string? value) => value is not null && WaitTypePattern().IsMatch(value);

        /// <summary>A deadlock signature as DBA Dash writes it: 0x followed by 16 hex digits.</summary>
        public static bool IsSignature(string? value) => value is not null && SignaturePattern().IsMatch(value);

        private static readonly Dictionary<string, string[]> Titles = new()
        {
            [Blocking] = ["Snapshots", "Root blockers, worst first", "What the blocked sessions waited on"],
            [WaitingQueries] = ["Snapshots", "Waiting queries, most sessions first"],
            [LongRunning] = ["Long running queries and transactions, longest first"],
            [Deadlock] = ["Occurrences in the window", "Processes of the latest occurrences", "Resources"]
        };

        /// <summary>
        /// The result sets as a model reads them: pipe-separated tables under a title, cut off at
        /// <paramref name="maxLength"/>.  <paramref name="formatTime"/> writes the times - in the reader's zone for the AI
        /// service, which says so in <paramref name="timeNote"/> and drops the UTC from the column names; in UTC for MCP.
        /// Statement text and the like are masked by column name as <paramref name="mask"/> says - parameter values and
        /// literals when it is left out.
        /// </summary>
        public static string Format(
            string summary,
            List<List<Dictionary<string, object?>>> sets,
            string lookup,
            Func<DateTime, string> formatTime,
            string timeNote,
            bool timesAreLocal,
            int maxLength,
            AiDataMask? mask = null)
        {
            var dataMask = mask ?? AiDataMask.Default;
            var text = new StringBuilder();
            text.AppendLine($"Lookup result: {summary}");
            text.AppendLine($"{timeNote}  Durations are milliseconds.  Snapshots are points in time, so durations are how long each had been running when seen.");
            var titles = Titles.TryGetValue(lookup, out var t) ? t : [];

            for (var i = 0; i < sets.Count; i++)
            {
                text.AppendLine();
                text.AppendLine(i < titles.Length ? $"{titles[i]}:" : $"Result {i + 1}:");
                var rows = sets[i];
                if (rows.Count == 0)
                {
                    text.AppendLine("(none)");
                    continue;
                }

                var columns = rows[0].Keys.ToList();
                text.AppendLine(string.Join(" | ", columns.Select(c => timesAreLocal && c.EndsWith("UTC", StringComparison.Ordinal) ? c[..^3] : c)));
                foreach (var row in rows)
                {
                    text.AppendLine(string.Join(" | ", columns.Select(c => Value(row[c] is string s ? AiDataMasker.MaskNamed(c, s, dataMask) : row[c], formatTime))));
                }
            }

            var result = text.ToString();
            return result.Length <= maxLength
                ? result
                : result[..maxLength] + Environment.NewLine + "(cut short - the rest of the result was left out)";
        }

        private static string Value(object? value, Func<DateTime, string> formatTime) => value switch
        {
            null => string.Empty,
            DateTime time => formatTime(time),
            bool b => b ? "yes" : "no",
            string s => Regex.Replace(s, @"\s+", " ").Replace("|", "/").Trim(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
