using DBADashAI.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DBADashAI.Services
{
    /// <summary>
    /// Lets a performance analysis look further before it answers: the model, having seen a lot of
    /// blocking or a deadlock signature that keeps recurring, asks for the detail behind it, and is
    /// given it as the next turn.
    ///
    /// The request is a fenced block of JSON in the model's reply rather than a provider's native tool
    /// call.  The service talks to four providers, each with its own tool calling format and some
    /// local models with none worth relying on; a block of text is the same everywhere, and costs a
    /// model nothing to write.  The price is that a model can get the format wrong, which is handled
    /// as a failed lookup the model is told about, not as an error.
    ///
    /// Everything it can reach is scoped to the instance and window of the analysis, with fixed row
    /// limits, so the model chooses what to look at but not how much: the lookups are read-only
    /// summaries from the AI schema, a handful of rows each.
    /// </summary>
    public sealed partial class PerformanceInvestigation
    {
        // The lookups themselves - procedures, parameters, how results read - are shared with the MCP server's tools; see
        // PerformanceLookups.  What is here is how this service's model asks for them.
        public const string BlockingTool = PerformanceLookups.Blocking;
        public const string WaitingQueriesTool = PerformanceLookups.WaitingQueries;
        public const string LongRunningTool = PerformanceLookups.LongRunning;
        public const string DeadlockTool = PerformanceLookups.Deadlock;

        /// <summary>Lookups in one answer.  Each is a round trip to the model as well as a query.</summary>
        public const int DefaultMaxLookups = 3;

        /// <summary>
        /// Time after which no lookup is started.  With the call in flight and the answer, the request can run two
        /// model calls past it; the GUI's wait allows for that.
        /// </summary>
        public const int DefaultDeadlineSeconds = 420;

        /// <summary>Said to a model that asks for a lookup when it can have no more.</summary>
        public const string NoMoreLookups = "No more lookups can be made.  Answer now, as asked, from what you have, with no investigate block.";

        /// <summary>What one lookup can add to the conversation, which is re-sent on every later turn.</summary>
        public const int MaxResultLength = 8 * 1024;

        private const string TimeFormat = "yyyy-MM-dd HH:mm";

        private readonly SqlToolExecutor _sql;

        public PerformanceInvestigation(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        /// <summary>Whether lookups can be offered for a request: asked for, scoped, and a repository to read.</summary>
        public bool CanInvestigate(AiPerformanceAnalysisRequest request) =>
            request.AllowInvestigation && request.InstanceId.HasValue && request.FromUtc.HasValue && request.ToUtc.HasValue && _sql.IsConfigured;

        /// <summary>What the model is told it can do, appended to the opening prompt.</summary>
        public static string Instructions(AiPerformanceAnalysisRequest request, int maxLookups)
        {
            var from = Local(request.FromUtc!.Value, request);
            var to = Local(request.ToUtc!.Value, request);
            var text = new StringBuilder();
            text.AppendLine();
            text.AppendLine("## Looking further");
            text.AppendLine(
                $"Before you answer you can look up more detail from DBA Dash, up to {maxLookups} times.  Do it only where the figures above point " +
                "at something the detail would explain - a lot of blocking, a deadlock signature that keeps recurring, tempdb or lock waits - and " +
                "where knowing which queries are involved would change what you recommend.  Ask for one lookup at a time.  If you are " +
                "about to tell the reader to find out which query, session or blocker it was, look it up instead and name it: the " +
                $"lookups read the same snapshots the reader would ({LongRunningTool} for a long query or old transaction).  In particular, " +
                $"before naming the cause of tempdb contention use {WaitingQueriesTool} with \"PAGELATCH%\" and \"tempdb_only\": true, before " +
                $"naming the cause of lock waits use {BlockingTool}, and name the procedures and statements those show.");
            text.AppendLine("To ask, reply with nothing but this block, and wait for the result:");
            text.AppendLine("```investigate");
            text.AppendLine($"{{\"tool\": \"{BlockingTool}\", \"from\": \"{from}\", \"to\": \"{to}\", \"reason\": \"why you want it, in a few words\"}}");
            text.AppendLine("```");
            text.AppendLine("Tools:");
            text.AppendLine($"- {BlockingTool}: the root blockers in the worst blocking snapshots - what they were running, which application, and whether they were sleeping with a transaction open - and what the blocked sessions waited on.");
            text.AppendLine($"- {WaitingQueriesTool}: the queries seen waiting on a wait type.  Needs \"wait_type\", a LIKE pattern such as \"PAGELATCH%\" or \"LCK_M%\".  Add \"tempdb_only\": true for waits on tempdb pages only.");
            text.AppendLine($"- {LongRunningTool}: the longest running queries and longest open transactions.");
            text.AppendLine($"- {DeadlockTool}: the processes and resources of the latest occurrences of one deadlock signature.  Needs \"signature\", as it appears in the Deadlocks section.");
            text.AppendLine($"\"from\" and \"to\" are optional, in the same time zone as the figures above and in the form {TimeFormat}; they narrow the lookup to a peak inside the window ({from} to {to}), which is used when they are left out.");
            text.AppendLine("When you have what you need, or need nothing more, answer as asked above, with no investigate block.  Say in the answer what the lookups showed.");
            return text.ToString();
        }

        [GeneratedRegex(@"```[ \t]*investigate\b(.*?)```", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex RequestBlock();

        /// <summary>
        /// The block a reply asks for a lookup with, when it has one.  Whatever is inside, JSON or not: a
        /// malformed request is for <see cref="Parse"/> to explain, so the model can ask again.
        /// </summary>
        public static string? FindRequest(string reply)
        {
            var match = RequestBlock().Match(reply);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        /// <summary>The reply with any lookup request taken out - for a model that asks again when it has been told it can't.</summary>
        public static string RemoveRequests(string reply) => RequestBlock().Replace(reply, string.Empty).Trim();

        /// <summary>
        /// Runs one lookup.  Returns the line the reader is shown and what the model is given back - which
        /// for a request it got wrong is what was wrong with it, so it can ask again properly.
        /// </summary>
        public async Task<(string Summary, string Result)> RunAsync(string json, AiPerformanceAnalysisRequest request, CancellationToken cancellationToken)
        {
            Lookup lookup;
            try
            {
                lookup = Parse(json, request);
            }
            catch (Exception ex) when (ex is JsonException or FormatException or ArgumentException)
            {
                // Said to the reader as well as the model: "could not be read" alone leaves nothing to go on.
                var reason = ex.Message.Length > 160 ? ex.Message[..160] + "..." : ex.Message;
                return ($"A lookup the model asked for could not be read: {reason}", $"That lookup could not be run: {ex.Message}");
            }

            var parameters = PerformanceLookups.Parameters(lookup.Tool, request.InstanceId!.Value, lookup.FromUtc, lookup.ToUtc,
                lookup.WaitType, lookup.TempDBOnly, lookup.Signature);
            var sets = await _sql.QueryMultiAsync(PerformanceLookups.Procedure(lookup.Tool), parameters, cancellationToken);

            var period = $"{Local(lookup.FromUtc, request)} to {Local(lookup.ToUtc, request)}";
            var summary = lookup.Tool switch
            {
                BlockingTool => $"Blocking, {period}",
                WaitingQueriesTool => $"Queries waiting on {lookup.WaitType}{(lookup.TempDBOnly ? " in tempdb" : string.Empty)}, {period}",
                LongRunningTool => $"Long running queries and transactions, {period}",
                _ => $"Deadlock signature {lookup.Signature}, {period}"
            };
            if (!string.IsNullOrWhiteSpace(lookup.Reason)) summary += $" - {lookup.Reason}";

            return (summary, Format(summary, sets, lookup.Tool, request));
        }

        internal sealed record Lookup(string Tool, DateTime FromUtc, DateTime ToUtc, string? WaitType, bool TempDBOnly, string? Signature, string? Reason);

        internal static Lookup Parse(string json, AiPerformanceAnalysisRequest request)
        {
            // Models write JSON loosely - a trailing comma, a comment saying what they want - and none of that changes what was asked.
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new FormatException("the block is not a JSON object.");

            var tool = Text(root, "tool")?.Trim().ToLowerInvariant();
            if (tool is not (BlockingTool or WaitingQueriesTool or LongRunningTool or DeadlockTool))
            {
                throw new FormatException($"\"tool\" must be one of {BlockingTool}, {WaitingQueriesTool}, {LongRunningTool} or {DeadlockTool}.");
            }

            // Inside the window whatever was asked: the model narrows a lookup, it does not widen one.
            var windowFrom = request.FromUtc!.Value;
            var windowTo = request.ToUtc!.Value;
            var from = Utc(Text(root, "from"), request) ?? windowFrom;
            var to = Utc(Text(root, "to"), request) ?? windowTo;
            if (from < windowFrom || from >= windowTo) from = windowFrom;
            if (to <= from || to > windowTo) to = windowTo;

            string? waitType = null;
            string? signature = null;
            if (tool == WaitingQueriesTool)
            {
                waitType = Text(root, "wait_type")?.Trim().ToUpperInvariant();
                if (!PerformanceLookups.IsWaitType(waitType))
                {
                    throw new FormatException("\"wait_type\" is required: a wait type, or a pattern such as PAGELATCH%.");
                }
            }
            else if (tool == DeadlockTool)
            {
                signature = Text(root, "signature")?.Trim();
                if (!PerformanceLookups.IsSignature(signature))
                {
                    throw new FormatException("\"signature\" is required, as 0x followed by 16 hex digits, as in the Deadlocks section.");
                }
            }

            var tempdb = root.TryGetProperty("tempdb_only", out var t) && t.ValueKind == JsonValueKind.True;
            var reason = Text(root, "reason");
            if (reason is { Length: > 200 }) reason = reason[..200];

            return new Lookup(tool, from, to, waitType, tempdb, signature, reason?.Trim());
        }

        private static string? Text(JsonElement root, string name) =>
            root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        /// <summary>A time as the model gives it - the reader's - in UTC.  Null when there is none or it can't be read: the window is used instead.</summary>
        internal static DateTime? Utc(string? local, AiPerformanceAnalysisRequest request) =>
            DateTime.TryParseExact(local?.Trim(), [TimeFormat, "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"],
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
                ? request.LocalToUtc(value)
                : null;

        /// <summary>A UTC time in the reader's zone, at the offset that applied then - the payload does the same.</summary>
        internal static string Local(DateTime utc, AiPerformanceAnalysisRequest request) =>
            utc.AddMinutes(request.OffsetAt(utc)).ToString(TimeFormat, CultureInfo.InvariantCulture);

        /// <summary>
        /// The result sets as the model is given them: times in the reader's zone like everything else it has, and cut
        /// off at <see cref="MaxResultLength"/>.
        /// </summary>
        internal static string Format(string summary, List<List<Dictionary<string, object?>>> sets, string tool, AiPerformanceAnalysisRequest request) =>
            PerformanceLookups.Format(summary, sets, tool, time => Local(time, request),
                "Times are in the same zone as the figures above.", timesAreLocal: true, MaxResultLength);

        /// <summary>What the model is told after a lookup: the result, and how many more it can have.</summary>
        public static string ResultTurn(string result, int remaining) =>
            result + Environment.NewLine + (remaining > 0
                ? $"You can make {remaining} more lookup{(remaining == 1 ? string.Empty : "s")}, or answer now."
                : "That was the last lookup.  Answer now, as asked, with no investigate block.");
    }
}
