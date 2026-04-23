using DBADashAI.Models;
using System.Text.RegularExpressions;

namespace DBADashAI.Services
{
    public partial class AiIntentRouter
    {
        public IReadOnlyList<IAiTool> SelectTools(AiAskRequest request, IEnumerable<IAiTool> tools)
        {
            // Extract instance filter and time window from the question if not already set
            request.InstanceFilter ??= ExtractInstanceFilter(request.Question);
            request.HoursBack ??= ExtractHoursBack(request.Question);

            var toolList = tools.ToList();

            if (!string.IsNullOrWhiteSpace(request.ToolName))
            {
                var explicitTool = toolList.FirstOrDefault(t => string.Equals(t.Name, request.ToolName, StringComparison.OrdinalIgnoreCase));
                return explicitTool is null ? [] : [explicitTool];
            }

            var q = request.Question.ToLowerInvariant();

            var scored = toolList
                .Select(t => new
                {
                    Tool = t,
                    Score = ComputeScore(q, t)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Tool.Name)
                .ToList();

            var wantsBroadTriage = q.Contains("root cause", StringComparison.OrdinalIgnoreCase)
                                   || q.Contains("triage", StringComparison.OrdinalIgnoreCase)
                                   || q.Contains("overall", StringComparison.OrdinalIgnoreCase)
                                   || q.Contains("big picture", StringComparison.OrdinalIgnoreCase)
                                   || q.Contains("health check", StringComparison.OrdinalIgnoreCase)
                                   || q.Contains("everything", StringComparison.OrdinalIgnoreCase)
                                   || (q.Contains("blocking", StringComparison.OrdinalIgnoreCase) && q.Contains("wait", StringComparison.OrdinalIgnoreCase))
                                   || (q.Contains("why", StringComparison.OrdinalIgnoreCase) && q.Contains("slow", StringComparison.OrdinalIgnoreCase));

            if (scored.Count == 0)
            {
                var fallback = toolList.FirstOrDefault(t => t.Name == "active-alerts-summary") ?? toolList.FirstOrDefault();
                return fallback is null ? [] : [fallback];
            }

            if (wantsBroadTriage)
            {
                return scored.Take(3).Select(x => x.Tool).ToList();
            }

            return [scored[0].Tool];
        }

        private static int ComputeScore(string question, IAiTool tool)
        {
            var score = tool.Keywords.Sum(k => question.Contains(k, StringComparison.OrdinalIgnoreCase) ? 1 : 0);

            if (tool.Name == "slow-queries-summary"
                && (question.Contains("regress", StringComparison.OrdinalIgnoreCase)
                    || question.Contains("query", StringComparison.OrdinalIgnoreCase)
                    || question.Contains("slow", StringComparison.OrdinalIgnoreCase)))
            {
                score += 3;
            }

            if (tool.Name == "waits-summary"
                && (question.Contains("wait profile", StringComparison.OrdinalIgnoreCase)
                    || question.Contains("lock contention", StringComparison.OrdinalIgnoreCase)
                    || question.Contains("contention", StringComparison.OrdinalIgnoreCase)))
            {
                score += 3;
            }

            if (tool.Name == "blocking-summary"
                && (question.Contains("block", StringComparison.OrdinalIgnoreCase)
                    || question.Contains("blocked", StringComparison.OrdinalIgnoreCase)
                    || question.Contains("blocking", StringComparison.OrdinalIgnoreCase)))
            {
                score += 5;
            }

            if (tool.Name == "instance-metadata-summary")
            {
                // Suppress metadata scoring when the question clearly targets another domain
                var hasDomainSignal = question.Contains("block", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("wait", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("deadlock", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("alert", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("backup", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("job", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("slow", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("pressure", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("timeframe", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("time frame", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("drift", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("config", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("reliability", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("restart", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("offline", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("failing", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("failed", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("hygiene", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("drive", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("disk", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("capacity", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("when did", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("how long", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("what happened", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("crash", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("outage", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("regression", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("query", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("performance", StringComparison.OrdinalIgnoreCase);

                if (!hasDomainSignal)
                {
                    var asksCount = question.Contains("how many", StringComparison.OrdinalIgnoreCase)
                                    || question.Contains("count", StringComparison.OrdinalIgnoreCase)
                                    || question.Contains("number of", StringComparison.OrdinalIgnoreCase);
                    var asksInventory = question.Contains("server", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("instance", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("version", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("edition", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("ram", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("memory", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("metadata", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("inventory", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("sql 2016", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("sql 2017", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("sql 2019", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("sql 2022", StringComparison.OrdinalIgnoreCase);

                    if (asksCount && asksInventory)
                    {
                        score += 5;
                    }
                    else if (asksInventory)
                    {
                        score += 3;
                    }
                }
            }

            if (tool.Name == "ag-dr-risk-summary")
            {
                var asksAgDr = question.Contains("availability group", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("ag ", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("replica", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("failover", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("dr", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("rpo", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("recoverability", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("hadr", StringComparison.OrdinalIgnoreCase);
                if (asksAgDr)
                {
                    score += 5;
                }
            }

            if (tool.Name == "workload-pressure-summary")
            {
                var asksPressure = question.Contains("pressure", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("contention", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("bottleneck", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("why are we slow", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("performance triage", StringComparison.OrdinalIgnoreCase)
                                   || (question.Contains("blocking", StringComparison.OrdinalIgnoreCase)
                                       && question.Contains("wait", StringComparison.OrdinalIgnoreCase));
                if (asksPressure)
                {
                    score += 5;
                }
            }

            if (tool.Name == "running-queries-summary")
            {
                var asksRunning = question.Contains("running quer", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("long running", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("long-running", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("currently running", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("head blocker", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("who is blocking", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("blocked by", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("memory grant", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("sleeping session", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("idle session", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("open transaction", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("forgotten transaction", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("what is running", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("active session", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("spid", StringComparison.OrdinalIgnoreCase);
                if (asksRunning)
                {
                    score += 6;
                }
            }

            if (tool.Name == "reliability-risk-summary")
            {
                var asksReliability = question.Contains("reliability", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("stable", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("unstable", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("restart", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("offline", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("job failure", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("recurring", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("incident", StringComparison.OrdinalIgnoreCase);
                if (asksReliability)
                {
                    score += 5;
                }
            }

            if (tool.Name == "capacity-forecast-summary")
            {
                var asksCapacity = question.Contains("capacity", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("runway", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("fill up", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("free space", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("growth", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("disk", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("drive", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("memory pressure", StringComparison.OrdinalIgnoreCase)
                                   || question.Contains("forecast", StringComparison.OrdinalIgnoreCase);
                if (asksCapacity)
                {
                    score += 5;
                }
            }

            if (tool.Name == "config-risk-drift-summary")
            {
                var asksConfigRisk = question.Contains("config risk", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("drift", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("configuration change", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("trace flag", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("patch", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("change review", StringComparison.OrdinalIgnoreCase)
                                     || question.Contains("what changed", StringComparison.OrdinalIgnoreCase);
                if (asksConfigRisk)
                {
                    score += 5;
                }
            }

            if (tool.Name == "config-current-summary")
            {
                var asksCurrentConfig = question.Contains("current config", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("what is maxdop", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("what is max memory", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("maxdop set to", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("set to", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("configured to", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("best practice", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("misconfigur", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("config audit", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("config review", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("compare config", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("xp_cmdshell", StringComparison.OrdinalIgnoreCase)
                                        || question.Contains("clr enabled", StringComparison.OrdinalIgnoreCase);
                if (asksCurrentConfig)
                {
                    score += 5;
                }
            }

            if (tool.Name == "storage-space-summary")
            {
                var asksStorage = question.Contains("drive space", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("disk space", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("drive growth", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("disk growth", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("days until full", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("database size", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("file size", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("log file", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("data file", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("autogrow", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("auto-grow", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("tempdb config", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("tempdb file", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("storage", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("allocated", StringComparison.OrdinalIgnoreCase);
                if (asksStorage)
                {
                    score += 5;
                }
            }

            if (tool.Name == "table-size-summary")
            {
                var asksTableSize = question.Contains("table size", StringComparison.OrdinalIgnoreCase)
                                    || question.Contains("largest table", StringComparison.OrdinalIgnoreCase)
                                    || question.Contains("biggest table", StringComparison.OrdinalIgnoreCase)
                                    || question.Contains("table growth", StringComparison.OrdinalIgnoreCase)
                                    || question.Contains("row count", StringComparison.OrdinalIgnoreCase)
                                    || question.Contains("row growth", StringComparison.OrdinalIgnoreCase)
                                    || question.Contains("index size", StringComparison.OrdinalIgnoreCase)
                                    || question.Contains("which table", StringComparison.OrdinalIgnoreCase)
                                    || question.Contains("growing table", StringComparison.OrdinalIgnoreCase)
                                    || question.Contains("space consumed", StringComparison.OrdinalIgnoreCase);
                if (asksTableSize)
                {
                    score += 5;
                }
            }

            if (tool.Name == "os-performance-summary")
            {
                var asksPerf = question.Contains("cpu", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("ple", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("page life", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("batch request", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("compilation", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("memory grant", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("performance counter", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("perf counter", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("top proc", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("top procedure", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("top cpu", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("how is the server", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("instance performance", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("user connection", StringComparison.OrdinalIgnoreCase)
                               || question.Contains("lazy write", StringComparison.OrdinalIgnoreCase);
                if (asksPerf)
                {
                    score += 6;
                }
            }

            if (tool.Name == "operational-hygiene-summary")
            {
                var asksHygiene = question.Contains("hygiene", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("acknowledge", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("unacknowledged", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("stale alert", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("backlog", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("alert debt", StringComparison.OrdinalIgnoreCase)
                                  || question.Contains("workflow", StringComparison.OrdinalIgnoreCase);
                if (asksHygiene)
                {
                    score += 5;
                }
            }

            if (tool.Name == "cross-signal-correlation-summary")
            {
                var asksCorrelation = question.Contains("correlation", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("cross-signal", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("systemic", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("multi signal", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("overall risk", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("root cause cluster", StringComparison.OrdinalIgnoreCase)
                                      || question.Contains("top risks", StringComparison.OrdinalIgnoreCase)
                                      || (question.Contains("why", StringComparison.OrdinalIgnoreCase)
                                          && question.Contains("slow", StringComparison.OrdinalIgnoreCase));
                if (asksCorrelation)
                {
                    score += 6;
                }
            }

            return score;
        }

        /// <summary>
        /// Extract an instance/server name from the question.
        /// Looks for patterns like SQL MI names, FQDN server names, or named instances (SERVER\INSTANCE).
        /// </summary>
        internal static string? ExtractInstanceFilter(string question)
        {
            // Match Azure SQL MI style: word-word-sqlmi or word.database.windows.net
            var azureMatch = AzureInstancePattern().Match(question);
            if (azureMatch.Success)
                return azureMatch.Value;

            // Match traditional named instance: SERVER\INSTANCE or SERVER\\INSTANCE
            var namedMatch = NamedInstancePattern().Match(question);
            if (namedMatch.Success)
                return namedMatch.Value.Replace("\\\\", "\\");

            // Match FQDN-like: server-name.domain.com
            var fqdnMatch = FqdnPattern().Match(question);
            if (fqdnMatch.Success)
                return fqdnMatch.Value;

            // Match hyphenated server names (3+ segments, at least one looks like a server name)
            var hyphenatedMatch = HyphenatedServerPattern().Match(question);
            if (hyphenatedMatch.Success)
            {
                var name = hyphenatedMatch.Value;
                // Only accept if it contains common server-name indicators
                if (name.Contains("sql", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("srv", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("db", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("edw", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("prod", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("dev", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("qa", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("uat", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("stg", StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }

            return null;
        }

        /// <summary>
        /// Parse natural-language time references into hours.
        /// </summary>
        internal static int? ExtractHoursBack(string question)
        {
            var q = question.ToLowerInvariant();

            // "last N hours/minutes/days/weeks"
            var match = TimeRangePattern().Match(q);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var n))
            {
                var unit = match.Groups[2].Value;
                return unit switch
                {
                    "minute" or "minutes" or "min" or "mins" => Math.Max(1, n / 60),
                    "hour" or "hours" or "hr" or "hrs" => n,
                    "day" or "days" => n * 24,
                    "week" or "weeks" => n * 168,
                    _ => null
                };
            }

            // Common phrases
            if (q.Contains("last hour")) return 1;
            if (q.Contains("past hour")) return 1;
            if (q.Contains("today")) return (int)(DateTime.UtcNow - DateTime.UtcNow.Date).TotalHours + 1;
            if (q.Contains("yesterday")) return 48;
            if (q.Contains("this week")) return 168;
            if (q.Contains("last week")) return 336;
            if (q.Contains("this month")) return 720;
            if (q.Contains("past 30 days") || q.Contains("last 30 days")) return 720;

            return null;
        }

        [GeneratedRegex(@"[a-zA-Z0-9\-]+\.database\.windows\.net", RegexOptions.IgnoreCase)]
        private static partial Regex AzureInstancePattern();

        [GeneratedRegex(@"[a-zA-Z0-9_\-]+\\{1,2}[a-zA-Z0-9_\-]+", RegexOptions.IgnoreCase)]
        private static partial Regex NamedInstancePattern();

        [GeneratedRegex(@"[a-zA-Z0-9\-]+\.[a-zA-Z0-9\-]+\.[a-zA-Z]{2,}", RegexOptions.IgnoreCase)]
        private static partial Regex FqdnPattern();

        [GeneratedRegex(@"[a-zA-Z0-9]+-[a-zA-Z0-9]+-[a-zA-Z0-9\-]+", RegexOptions.IgnoreCase)]
        private static partial Regex HyphenatedServerPattern();

        [GeneratedRegex(@"(?:last|past)\s+(\d+)\s+(minute|minutes|min|mins|hour|hours|hr|hrs|day|days|week|weeks)", RegexOptions.IgnoreCase)]
        private static partial Regex TimeRangePattern();
    }
}
