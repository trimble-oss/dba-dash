using Humanizer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using static DBADashGUI.Performance.QueryInsights;

namespace DBADashGUI.Performance
{
    /// <summary>In-app actions the snapshot insights can link to.</summary>
    internal sealed class SnapshotInsightActions
    {
        /// <summary>Open the session detail viewer for a session in the snapshot.</summary>
        public Action<int> OpenSession { get; init; }

        /// <summary>Filter the grid to the root blockers.</summary>
        public Action ShowRootBlockers { get; init; }

        /// <summary>Filter the grid to the queries blocked by another session.</summary>
        public Action ShowBlockedQueries { get; init; }

        /// <summary>Filter the grid with a DataView row filter. The second argument describes the filter.</summary>
        public Action<string, string> ApplyFilter { get; init; }
    }

    /// <summary>
    /// Insights for a whole running queries snapshot - the snapshot-level counterpart of the insights on the
    /// <see cref="SessionDetailViewer"/> Overview tab. Similar issues are summarized across all the sessions in the
    /// snapshot, with links to open a session or to filter the grid to the sessions involved.
    /// </summary>
    internal static class SnapshotInsights
    {
        // The number of sessions named individually in an insight before the rest are summarized as "and N more".
        private const int MaxSessionsListed = 3;

        private const string AllocationPageFilter = "page_type IN ('PFS','GAM','SGAM')";
        private const string PageLatchFilter = "wait_type LIKE 'PAGELATCH_%'";

        /// <param name="snapshot">All the sessions in the snapshot.</param>
        /// <param name="actions">In-app actions the insight links invoke.</param>
        /// <param name="includeSummary">Add a summary of the snapshot's workload (statuses, waits, jobs etc.) as the last card.</param>
        public static List<Insight> Build(DataTable snapshot, SnapshotInsightActions actions, bool includeSummary = true)
        {
            var list = new List<Insight>();
            if (snapshot == null || snapshot.Rows.Count == 0)
            {
                list.Add(new Insight(InsightSeverity.Info, "No queries were captured in this snapshot.") { IsSummary = true });
                return list;
            }

            var rows = snapshot.AsEnumerable().ToList();
            AddBlocking(list, rows, actions);
            AddSleepingOpenTransactions(list, rows, actions);
            AddMemoryGrants(list, rows, actions);
            AddAllocationContention(list, rows, actions);
            AddTempDbWaits(list, rows, actions);
            AddWaitTypeInsights(list, rows, actions);
            AddImplicitTransactions(list, rows, actions);

            var noIssues = list.Count == 0;
            // Most severe first (OrderByDescending is stable, so related insights keep their relative order).
            list = list.OrderByDescending(i => i.Severity).ToList();

            // The summary (or a plain "no issues" message when it's turned off) goes last and isn't counted as an insight.
            if (includeSummary)
            {
                list.Add(BuildSummary(rows, actions, noIssues));
            }
            else if (noIssues)
            {
                list.Add(new Insight(InsightSeverity.Info,
                    "No issues were automatically detected in this snapshot. Click a Session ID for insights on an individual query.") { IsSummary = true });
            }
            return list;
        }

        /// <summary>
        /// A summary of the workload in the snapshot so the user gets a picture of what's running at a glance - useful
        /// context alongside the insights, or on its own when no issues were detected. Not counted as an insight.
        /// </summary>
        private static Insight BuildSummary(List<DataRow> rows, SnapshotInsightActions actions, bool noIssues)
        {
            var links = new Links(actions);
            var lines = new List<string>();

            // Sessions by status (running / runnable / suspended / sleeping...)
            var statuses = rows.GroupBy(r => Str(r, "status").ToLowerInvariant())
                .Where(g => g.Key != string.Empty)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Count()} {g.Key}")
                .ToList();
            lines.Add($"{Pluralize(rows.Count, "session", "sessions")} captured" +
                      (statuses.Count == 0 ? "." : $": {string.Join(", ", statuses)}."));

            // What the queries are waiting on, most common first.
            var waits = rows.Where(r => WaitType(r) != string.Empty)
                .GroupBy(WaitType)
                .Select(g => (Type: g.Key, Count: g.Count(), MaxWait: g.Max(r => Dbl(r, "wait_time"))))
                .OrderByDescending(w => w.Count)
                .ThenByDescending(w => w.MaxWait)
                .ToList();
            lines.Add(waits.Count == 0
                ? "No queries are waiting."
                : "Waiting on: " + JoinList(waits.Select(w =>
                {
                    var longest = HumanizeMs(w.MaxWait);
                    return $"{WaitTypeLink(w.Type)} ({Pluralize(w.Count, "query", "queries")}{(longest == null ? string.Empty : $", longest {longest}")})";
                }).ToList(), "other wait type", "other wait types") + ".");

            // Longest running request (sleeping sessions aren't running anything; background tasks are SQL Server's own).
            var longestRunning = rows.Where(r => Str(r, "status").ToLowerInvariant() is not ("sleeping" or "background"))
                .OrderByDescending(r => Dbl(r, "Duration (ms)"))
                .FirstOrDefault();
            var longestDuration = longestRunning == null ? null : HumanizeMs(Dbl(longestRunning, "Duration (ms)"));
            if (longestDuration != null)
            {
                lines.Add($"Longest running query: session {links.Session(longestRunning)} ({longestDuration}).");
            }

            var openTran = rows.Where(r => Int(r, "open_transaction_count") > 0)
                .OrderByDescending(r => Dbl(r, "transaction_duration_ms"))
                .ToList();
            if (openTran.Count > 0)
            {
                var oldest = HumanizeMs(Dbl(openTran[0], "transaction_duration_ms"));
                lines.Add($"{Pluralize(openTran.Count, "session has", "sessions have")} an open transaction" +
                          (oldest == null ? "." : $" - the oldest is session {links.Session(openTran[0])} ({oldest})."));
            }

            var jobs = rows.Where(r => r.Table.Columns.Contains("job_id") && r["job_id"] != DBNull.Value)
                .GroupBy(r => r["job_id"])
                .Select(g => Str(g.First(), "job_name") is { Length: > 0 } name ? name : Convert.ToString(g.Key))
                .OrderBy(name => name)
                .ToList();
            if (jobs.Count > 0)
            {
                lines.Add($"{Pluralize(jobs.Count, "SQL Agent job", "SQL Agent jobs")} running: {JoinList(jobs, "other", "others")}.");
            }

            var parallel = rows.Where(r => Int(r, "dop") > 1).ToList();
            if (parallel.Count > 0)
            {
                lines.Add($"{Pluralize(parallel.Count, "query is", "queries are")} running in parallel (max DOP {parallel.Max(r => Int(r, "dop"))}).");
            }

            var grants = rows.Where(r => Dbl(r, "granted_query_memory_kb") > 0)
                .OrderByDescending(r => Dbl(r, "granted_query_memory_kb"))
                .ToList();
            if (grants.Count > 0)
            {
                lines.Add($"Memory granted: {HumanizeKb(grants.Sum(r => Dbl(r, "granted_query_memory_kb")))} across {Pluralize(grants.Count, "query", "queries")} " +
                          $"(largest: session {links.Session(grants[0])}, {HumanizeKb(Dbl(grants[0], "granted_query_memory_kb"))}).");
            }

            var tempDbMb = rows.Sum(r => Dbl(r, "tempdb_current_mb"));
            if (tempDbMb > 0)
            {
                lines.Add($"tempdb currently in use by these queries: {HumanizeKb(tempDbMb * 1024)}.");
            }

            var text = (noIssues ? "No issues were automatically detected in this snapshot.\n" : "Snapshot summary:\n") +
                       string.Join("\n", lines.Select(l => "• " + l)) +
                       "\nClick a Session ID for insights on an individual query.";
            return new Insight(InsightSeverity.Info, text, links.Actions) { IsSummary = true };
        }

        private static void AddBlocking(List<Insight> list, List<DataRow> rows, SnapshotInsightActions actions)
        {
            // Only a positive blocking_session_id is blocking by another session - the same rule as the blocked counts in
            // the grid and the repository's snapshot summary. -2/-3 (blocking by a transaction with no session) are
            // reported separately below; -4/-5 are page latch waits whose latch owner isn't known or tracked - not
            // blocking (latch contention is covered by the allocation/tempdb insights).
            var blocked = rows.Where(r => Int(r, "blocking_session_id") > 0).ToList();
            if (blocked.Count > 0)
            {
                var links = new Links(actions);
                var totalWait = HumanizeMs(blocked.Sum(r => Dbl(r, "wait_time")));
                var text = $"{Pluralize(blocked.Count, "query is", "queries are")} blocked" +
                           (totalWait == null ? "." : $" for a total of {totalWait}.");

                // Root blockers hold the locks at the head of each blocking chain without being blocked themselves.
                var roots = rows.Where(r => Int(r, "blocking_session_id") == 0 && Int(r, "BlockCount") > 0)
                    .OrderByDescending(r => Math.Max(Int(r, "BlockCountRecursive"), Int(r, "BlockCount")))
                    .ToList();
                if (roots.Count == 1)
                {
                    text += $" The root blocker is {RootBlockerText(roots[0], links)}.";
                }
                else if (roots.Count > 1)
                {
                    text += $" There are {roots.Count} root blockers: {JoinSessions(roots, r => RootBlockerText(r, links))}.";
                }

                if (roots.Any(IsSleepingWithOpenTran))
                {
                    text += " A sleeping root blocker means SQL Server is waiting for the application to submit more work while a transaction has been left open.";
                }

                text += "\n" + (roots.Count > 0 ? links.Action("Show root blockers", "root-blockers", actions.ShowRootBlockers) + "    " : string.Empty) +
                        links.Action("Show blocked queries", "blocked", actions.ShowBlockedQueries);
                list.Add(new Insight(InsightSeverity.Critical, text, links.Actions));
            }

            // Blocking by a transaction that has no session (orphaned DTC / deferred recovery) - there's no blocker to open.
            foreach (var special in rows.Where(r => SpecialBlockerDescription(Int(r, "blocking_session_id")) != null)
                         .GroupBy(r => Int(r, "blocking_session_id")))
            {
                var victims = special.OrderByDescending(r => Dbl(r, "wait_time")).ToList();
                var links = new Links(actions);
                var subject = victims.Count == 1
                    ? $"Session {links.Session(victims[0])} is"
                    : $"{victims.Count} queries are";
                var text = $"{subject} blocked{LongestWaitedSuffix(Dbl(victims[0], "wait_time"), victims.Count)} by {SpecialBlockerDescription(special.Key)}.";
                if (victims.Count > 1)
                {
                    text += "\n" + links.Filter("Show queries", $"blocker{special.Key}", $"blocking_session_id = {special.Key}",
                        $"Blocked by {special.Key}");
                }
                list.Add(new Insight(InsightSeverity.Critical, text, links.Actions));
            }
        }

        private static string RootBlockerText(DataRow row, Links links)
        {
            var blockCount = Math.Max(Int(row, "BlockCountRecursive"), Int(row, "BlockCount"));
            var detail = $"blocking {Pluralize(blockCount, "query", "queries")}";
            if (IsSleepingWithOpenTran(row))
            {
                var idle = IdleText(row);
                detail += idle == null ? ", sleeping with an open transaction" : $", sleeping with an open transaction for {idle}";
            }
            return $"session {links.Session(row)} ({detail})";
        }

        private static void AddSleepingOpenTransactions(List<Insight> list, List<DataRow> rows, SnapshotInsightActions actions)
        {
            // Sleeping sessions that are blocking are already covered by the blocking insight.
            var sleeping = rows.Where(r => IsSleepingWithOpenTran(r) && Int(r, "BlockCountRecursive") == 0 && Int(r, "BlockCount") == 0)
                .OrderByDescending(r => Dbl(r, "sleeping_session_idle_time_sec"))
                .ToList();
            if (sleeping.Count == 0) return;

            var maxIdleSec = Dbl(sleeping[0], "sleeping_session_idle_time_sec");
            // Brief idle periods inside a transaction are common, so this is only informational until the idle time
            // crosses the configured warning threshold (the same thresholds used to colour the Idle Time column).
            var severity = maxIdleSec >= Config.IdleCriticalThresholdForSleepingSessionWithOpenTran
                ? InsightSeverity.Critical
                : maxIdleSec >= Config.IdleWarningThresholdForSleepingSessionWithOpenTran
                    ? InsightSeverity.Warning
                    : InsightSeverity.Info;

            var links = new Links(actions);
            var subject = sleeping.Count == 1
                ? $"Session {links.Session(sleeping[0])} is"
                : $"{sleeping.Count} sessions are";
            var idle = IdleText(sleeping[0]);
            var idleText = idle == null ? string.Empty : sleeping.Count == 1 ? $" (idle for {idle})" : $" (the longest idle for {idle})";
            var text = $"{subject} sleeping with an open transaction{idleText}. This can cause blocking or prevent transaction log truncation. Sleeping sessions with open transactions usually indicate an application issue.";
            if (sleeping.Count > 1)
            {
                text += "\n" + links.Filter("Show sessions", "sleeping",
                    "status = 'sleeping' AND open_transaction_count > 0 AND ISNULL(BlockCount, 0) = 0 AND ISNULL(BlockCountRecursive, 0) = 0",
                    "Sleeping with open transaction");
            }
            list.Add(new Insight(severity, text, links.Actions));
        }

        private static void AddMemoryGrants(List<Insight> list, List<DataRow> rows, SnapshotInsightActions actions)
        {
            var waiters = rows.Where(r => WaitType(r) == "RESOURCE_SEMAPHORE").ToList();
            var largeGrants = rows.Where(r => Dbl(r, "granted_query_memory_kb") >= LargeMemoryGrantKB)
                .OrderByDescending(r => Dbl(r, "granted_query_memory_kb"))
                .ToList();
            var links = new Links(actions);

            if (waiters.Count > 0)
            {
                var maxWait = waiters.Max(r => Dbl(r, "wait_time"));
                var subject = waiters.Count == 1
                    ? $"Session {links.Session(waiters[0])} is"
                    : $"{waiters.Count} queries are";
                var text = $"{subject} in a queue waiting for a memory grant before {(waiters.Count == 1 ? "it" : "they")} can run ({WaitTypeLink("RESOURCE_SEMAPHORE")}){LongestWaitedSuffix(maxWait, waiters.Count)}.";

                // Queries holding the largest grants are the most likely cause of the queue.
                var topGrants = rows.Where(r => Dbl(r, "granted_query_memory_kb") > 0)
                    .OrderByDescending(r => Dbl(r, "granted_query_memory_kb"))
                    .Take(MaxSessionsListed)
                    .ToList();
                if (topGrants.Count > 0)
                {
                    text += $" The largest memory grants in this snapshot are held by {string.Join(", ", topGrants.Select(r => $"session {links.Session(r)} ({HumanizeKb(Dbl(r, "granted_query_memory_kb"))})"))}.";
                }
                text += " Look for queries running with large memory grants and reduce them (e.g. eliminate unnecessary sorts) or consider adding more memory.";
                if (waiters.Count > 1)
                {
                    text += "\n" + links.Filter("Show waiting queries", "memory-waiters", "wait_type = 'RESOURCE_SEMAPHORE'", "Waiting for memory grant");
                }
                list.Add(new Insight(WaitSeverity(maxWait, MemoryGrantCriticalMs), text, links.Actions));
            }
            else if (largeGrants.Count > 0)
            {
                // Only a warning while no other queries are queued waiting for memory.
                var subject = largeGrants.Count == 1
                    ? $"Session {links.Session(largeGrants[0])} has a large memory grant of {HumanizeKb(Dbl(largeGrants[0], "granted_query_memory_kb"))}."
                    : $"{largeGrants.Count} queries have large memory grants (at least {HumanizeKb(LargeMemoryGrantKB)}), totalling {HumanizeKb(largeGrants.Sum(r => Dbl(r, "granted_query_memory_kb")))}. The largest are {JoinSessions(largeGrants, r => $"session {links.Session(r)} ({HumanizeKb(Dbl(r, "granted_query_memory_kb"))})")}.";
                var text = subject + $" No queries are currently waiting for a memory grant, but large grants reserve memory that other queries can't use and can lead to {WaitTypeLink("RESOURCE_SEMAPHORE")} waits.";
                if (largeGrants.Count > 1)
                {
                    text += "\n" + links.Filter("Show queries", "large-grants",
                        $"granted_query_memory_kb >= {LargeMemoryGrantKB.ToString(CultureInfo.InvariantCulture)}", "Large memory grant");
                }
                list.Add(new Insight(InsightSeverity.Warning, text, links.Actions));
            }
        }

        private static void AddAllocationContention(List<Insight> list, List<DataRow> rows, SnapshotInsightActions actions)
        {
            // Group allocation-page latch waits by database - contention in tempdb and in a user database have different fixes.
            var groups = rows.Where(r => IsAllocationContention(Str(r, "page_type"), Str(r, "wait_type")))
                .GroupBy(r => (DatabaseId: Int(r, "wait_database_id"), Database: Str(r, "wait_db")));

            foreach (var group in groups)
            {
                var sessions = group.OrderByDescending(r => Dbl(r, "wait_time")).ToList();
                var maxWait = Dbl(sessions[0], "wait_time");
                var widespread = sessions.Count >= AllocationContentionWaiterThreshold;

                var isTempDb = group.Key.DatabaseId == 2 || string.Equals(group.Key.Database, "tempdb", StringComparison.OrdinalIgnoreCase);
                var db = isTempDb ? "tempdb" : string.IsNullOrEmpty(group.Key.Database) ? "a database" : group.Key.Database;
                var remedy = isTempDb
                    ? " Common solutions include ensuring tempdb has multiple, evenly-sized data files, tuning the workload to write less data to tempdb, and updating to a later CU / version of SQL Server."
                    : " Common solutions include adding more, evenly-sized data files to the filegroup, spreading the inserts across multiple objects, and avoiding sequential/hotspot inserts into a single heap or clustered index.";
                var pageTypes = string.Join("/", sessions.Select(r => Str(r, "page_type")).Distinct().OrderBy(p => p));

                var links = new Links(actions);
                var subject = sessions.Count == 1
                    ? $"Session {links.Session(sessions[0])} is"
                    : $"{sessions.Count} queries are";
                InsightSeverity severity;
                string text;
                if (maxWait <= 0 && !widespread)
                {
                    // A brief hit with no recorded wait time is most likely a transient blip - surface it for awareness only.
                    severity = InsightSeverity.Info;
                    text = $"{subject} on {pageTypes} allocation pages in {db} but no wait time was recorded. This looks like a transient blip rather than sustained allocation contention and can probably be ignored - check later snapshots if it persists.";
                }
                else
                {
                    severity = maxWait >= AllocationCriticalMs ? InsightSeverity.Critical : InsightSeverity.Warning;
                    text = $"{subject} waiting on {pageTypes} allocation pages in {db}{LongestWaitedSuffix(maxWait, sessions.Count)}, which looks like allocation contention.{remedy}";
                }
                if (sessions.Count > 1)
                {
                    var dbFilter = group.Key.DatabaseId > 0
                        ? $"wait_database_id = {group.Key.DatabaseId}"
                        : $"wait_db = '{group.Key.Database.Replace("'", "''")}'";
                    text += "\n" + links.Filter("Show queries", "allocation", $"{AllocationPageFilter} AND {PageLatchFilter} AND {dbFilter}", $"Allocation contention in {db}");
                }
                list.Add(new Insight(severity, text, links.Actions));
            }
        }

        private static void AddTempDbWaits(List<Insight> list, List<DataRow> rows, SnapshotInsightActions actions)
        {
            // Page latch waits in tempdb that aren't on an allocation page (those are covered by the allocation contention
            // insight). Lock waits on tempdb objects are blocking (covered by the blocking insight) and PAGEIOLATCH is I/O,
            // so neither is treated as tempdb contention.
            var sessions = rows.Where(r =>
                    (Int(r, "wait_database_id") == 2 || string.Equals(Str(r, "wait_db"), "tempdb", StringComparison.OrdinalIgnoreCase)) &&
                    IsPageLatchWait(Str(r, "wait_type")) &&
                    !IsAllocationPage(Str(r, "page_type")))
                .OrderByDescending(r => Dbl(r, "wait_time"))
                .ToList();
            if (sessions.Count == 0) return;

            var maxWait = Dbl(sessions[0], "wait_time");
            var links = new Links(actions);
            var subject = sessions.Count == 1
                ? $"Session {links.Session(sessions[0])} is"
                : $"{sessions.Count} queries are";
            var noWait = maxWait <= 0;
            var text = noWait
                ? $"{subject} waiting on a page latch in tempdb but no wait time was recorded. With no measurable wait this is likely a transient blip - check later snapshots if it persists. {TempDbPageLatchExplanation}"
                : $"{subject} waiting on a page latch in tempdb{LongestWaitedSuffix(maxWait, sessions.Count)}. {TempDbPageLatchExplanation}";
            if (sessions.Count > 1)
            {
                text += "\n" + links.Filter("Show queries", "tempdb",
                    $"(wait_database_id = 2 OR wait_db = 'tempdb') AND {PageLatchFilter} AND (page_type IS NULL OR NOT ({AllocationPageFilter}))", "tempdb page latch waits");
            }
            list.Add(new Insight(noWait ? InsightSeverity.Info : WaitSeverity(maxWait, TempDbCriticalMs), text, links.Actions));
        }

        private static void AddWaitTypeInsights(List<Insight> list, List<DataRow> rows, SnapshotInsightActions actions)
        {
            AddWaitInsight(list, rows.Where(r => Bool(r, "wait_is_compile")).ToList(), actions,
                CompileLockCriticalMs, "wait_is_compile = true", "Compile lock",
                (subject, waited) => $"{subject} blocked waiting on a compile lock{waited}. {CompileLockExplanation}");

            AddWaitInsight(list, rows.Where(r => WaitType(r) == "RESOURCE_SEMAPHORE_QUERY_COMPILE").ToList(), actions,
                CompileMemoryCriticalMs, "wait_type = 'RESOURCE_SEMAPHORE_QUERY_COMPILE'", "Waiting for compile memory",
                (subject, waited) => $"{subject} waiting for memory to compile a plan ({WaitTypeLink("RESOURCE_SEMAPHORE_QUERY_COMPILE")}){waited}. {CompileMemoryExplanation}");

            AddWaitInsight(list, rows.Where(r => WaitType(r) == "ASYNC_NETWORK_IO").ToList(), actions,
                AsyncNetworkIoCriticalMs, "wait_type = 'ASYNC_NETWORK_IO'", "ASYNC_NETWORK_IO",
                (subject, waited) => $"{subject} waiting on {WaitTypeLink("ASYNC_NETWORK_IO")}{waited}. This usually means the client application isn't consuming the results fast enough (for example processing rows one at a time, or a slow/overloaded client) rather than a SQL Server problem.");
        }

        /// <summary>Add an insight for the sessions experiencing a particular kind of wait (if any).</summary>
        private static void AddWaitInsight(List<Insight> list, List<DataRow> sessions, SnapshotInsightActions actions,
            double criticalMs, string filter, string filterDescription, Func<string, string, string> describe)
        {
            if (sessions.Count == 0) return;
            sessions = sessions.OrderByDescending(r => Dbl(r, "wait_time")).ToList();
            var maxWait = Dbl(sessions[0], "wait_time");
            var links = new Links(actions);
            var subject = sessions.Count == 1
                ? $"Session {links.Session(sessions[0])} is"
                : $"{sessions.Count} queries are";
            var text = describe(subject, LongestWaitedSuffix(maxWait, sessions.Count));
            if (sessions.Count > 1)
            {
                text += "\n" + links.Filter("Show queries", "wait", filter, filterDescription);
            }
            list.Add(new Insight(WaitSeverity(maxWait, criticalMs), text, links.Actions));
        }

        private static void AddImplicitTransactions(List<Insight> list, List<DataRow> rows, SnapshotInsightActions actions)
        {
            var sessions = rows.Where(r => Bool(r, "is_implicit_transaction")).ToList();
            if (sessions.Count == 0) return;

            var links = new Links(actions);
            var subject = sessions.Count == 1
                ? $"Session {links.Session(sessions[0])} is"
                : $"{sessions.Count} sessions are";
            var text = $"{subject} using implicit transactions, which are best avoided. Transactions can be started without an explicit BEGIN TRAN and may be left open unintentionally, causing blocking or log growth.";
            if (sessions.Count > 1)
            {
                text += "\n" + links.Filter("Show sessions", "implicit", "is_implicit_transaction = true", "Implicit transactions");
            }
            list.Add(new Insight(InsightSeverity.Warning, text, links.Actions));
        }

        /// <summary>" (waiting X)" for a single session, or " (longest waiting X)" when there are several.</summary>
        private static string LongestWaitedSuffix(double waitMs, int count)
        {
            var human = HumanizeMs(waitMs);
            if (human == null) return string.Empty;
            return count == 1 ? $" (waiting {human})" : $" (longest waiting {human})";
        }

        /// <summary>The first few sessions formatted with <paramref name="format"/>, then "and N more".</summary>
        private static string JoinSessions(IReadOnlyList<DataRow> sessions, Func<DataRow, string> format)
        {
            var listed = sessions.Take(MaxSessionsListed).Select(format).ToList();
            var more = sessions.Count - listed.Count;
            if (more > 0)
            {
                listed.Add($"{more} more");
            }
            return JoinWithAnd(listed);
        }

        /// <summary>The first few items, then "and N others" (using the given singular/plural for the remainder).</summary>
        private static string JoinList(IReadOnlyList<string> items, string otherSingular, string otherPlural)
        {
            var listed = items.Take(MaxSessionsListed).ToList();
            var more = items.Count - listed.Count;
            if (more > 0)
            {
                listed.Add(Pluralize(more, otherSingular, otherPlural));
            }
            return JoinWithAnd(listed);
        }

        private static string JoinWithAnd(IReadOnlyList<string> items) =>
            items.Count == 1 ? items[0] : string.Join(", ", items.Take(items.Count - 1)) + " and " + items[^1];

        private static bool IsSleepingWithOpenTran(DataRow row) =>
            string.Equals(Str(row, "status"), "sleeping", StringComparison.OrdinalIgnoreCase) &&
            Int(row, "open_transaction_count") > 0;

        private static string IdleText(DataRow row)
        {
            var idleText = Str(row, "sleeping_session_idle_time");
            if (!string.IsNullOrEmpty(idleText)) return idleText;
            var idleSec = Dbl(row, "sleeping_session_idle_time_sec");
            return idleSec > 0 ? TimeSpan.FromSeconds(idleSec).Humanize(precision: 2, maxUnit: TimeUnit.Day) : null;
        }

        private static string WaitType(DataRow row) => Str(row, "wait_type").ToUpperInvariant();

        private static int Int(DataRow row, string column) =>
            row.Table.Columns.Contains(column) ? Convert.ToInt32(row[column].DBNullToNull() ?? 0) : 0;

        private static double Dbl(DataRow row, string column) =>
            row.Table.Columns.Contains(column) ? Convert.ToDouble(row[column].DBNullToNull() ?? 0) : 0;

        private static string Str(DataRow row, string column) =>
            row.Table.Columns.Contains(column) ? Convert.ToString(row[column].DBNullToNull()) ?? string.Empty : string.Empty;

        private static bool Bool(DataRow row, string column) =>
            row.Table.Columns.Contains(column) && row[column].DBNullToNull() is { } value && Convert.ToBoolean(value);

        /// <summary>Builds the in-app action links for one insight and collects the actions they invoke.</summary>
        private sealed class Links
        {
            private readonly SnapshotInsightActions actions;
            private readonly Dictionary<string, Action> map = new(StringComparer.OrdinalIgnoreCase);

            public Links(SnapshotInsightActions actions) => this.actions = actions;

            public IReadOnlyDictionary<string, Action> Actions => map;

            /// <summary>A "session N" link that opens the session detail viewer.</summary>
            public string Session(DataRow row)
            {
                var sessionId = Int(row, "session_id");
                map[$"open-{sessionId}"] = () => actions.OpenSession?.Invoke(sessionId);
                return $"[{sessionId}](action:open-{sessionId})";
            }

            public string Action(string text, string key, Action action)
            {
                map[key] = action;
                return $"[{text}](action:{key})";
            }

            public string Filter(string text, string key, string filter, string description) =>
                Action(text, key, () => actions.ApplyFilter?.Invoke(filter, description));
        }
    }
}
