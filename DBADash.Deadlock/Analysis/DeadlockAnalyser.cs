using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock.Analysis
{
    /// <summary>
    /// Reads a parsed deadlock graph and says what kind of deadlock it is.
    ///
    /// The picture shows what happened; this is the part that says why, and what usually stops it
    /// happening again.  Every rule here is decidable from the graph alone - nothing goes back to the
    /// instance - so findings are available for a graph opened from a file, from a report, or from an
    /// instance nobody is monitoring.
    ///
    /// The rules are deliberately conservative.  A deadlock graph shows what the engine saw, not what
    /// the application meant, so a finding says what the graph shows first and offers the usual
    /// remedy second, hedged where it deserves hedging.  A confident wrong answer here would cost
    /// somebody a day.
    /// </summary>
    public static class DeadlockAnalyser
    {
        /// <summary>Log used by the victim above which its rollback is worth remarking on.</summary>
        private const long NotableLogUsed = 1024 * 1024;

        public static IReadOnlyList<DeadlockFinding> Analyse(DeadlockGraph graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var findings = new List<DeadlockFinding>();

            findings.AddRange(ConversionDeadlocks(graph));
            findings.AddRange(KeyLookupDeadlocks(graph));
            findings.AddRange(OppositeAccessOrder(graph));
            findings.AddRange(LongerCycle(graph));
            findings.AddRange(ParallelDeadlock(graph));
            findings.AddRange(SameModuleBothSides(graph));
            findings.AddRange(BlockedByReader(graph));
            findings.AddRange(IsolationLevels(graph));
            findings.AddRange(TableLevelLocks(graph));
            findings.AddRange(HeapLocks(graph));
            findings.AddRange(ApplicationLocks(graph));
            findings.AddRange(DeadlockPriority(graph));
            findings.AddRange(VictimCost(graph));
            findings.AddRange(CaptureProblems(graph));

            // Warnings about the capture first - they change how much weight the rest deserves -
            // then the advice, then the observations.
            return findings
                .OrderByDescending(f => f.Severity == DeadlockFindingSeverity.Warning)
                .ThenByDescending(f => f.Severity == DeadlockFindingSeverity.Advice)
                .ToList();
        }

        // ---------------------------------------------------------------- patterns

        /// <summary>
        /// A process that both holds and is waiting to convert the same resource: the read-then-write
        /// pattern, which is the most common deadlock there is and the one with the clearest fix.
        /// </summary>
        private static IEnumerable<DeadlockFinding> ConversionDeadlocks(DeadlockGraph graph)
        {
            foreach (var resource in graph.Resources)
            {
                var converting = resource.Waiters
                    .Where(w => w.Process is not null &&
                                resource.Owners.Any(o => ReferenceEquals(o.Process, w.Process)))
                    .ToList();

                if (converting.Count == 0) continue;

                var processes = converting.Select(w => w.Process!).ToList();
                var heldParticipants = resource.Owners.Where(o => processes.Contains(o.Process!)).ToList();
                var held = Modes(heldParticipants);
                var wanted = Modes(converting);

                // The read-then-write story only holds when the lock being converted started out as a
                // shared or update lock on a row, page or key.  An intent lock on a table converting to
                // X is lock escalation - a different problem - so describe that case plainly instead of
                // claiming a read of the same row.
                var readThenWrite =
                    heldParticipants.All(o => IsSharedOrUpdate(o.Mode)) &&
                    resource.Type is DeadlockResourceType.KeyLock
                        or DeadlockResourceType.RidLock
                        or DeadlockResourceType.PageLock;

                var explanation = readThenWrite
                    ? $"That is a read followed by an update of the same {RowOrResource(resource)} inside one " +
                      "transaction: each side takes the shared lock, then neither can have the exclusive one.  " +
                      "Taking the update lock on the read (UPDLOCK), or doing the read and the write as a single " +
                      "statement, removes the window."
                    : "Each side holds the resource in a mode that is compatible with the other, then asks to " +
                      "convert to one that is not, so neither conversion can complete.  Taking the stronger lock up " +
                      "front (UPDLOCK on the initial read), or reading and writing in a single statement, removes " +
                      "the window where both sides hold it.";

                yield return new DeadlockFinding
                {
                    Severity = DeadlockFindingSeverity.Advice,
                    Title = "Lock conversion deadlock",
                    Detail =
                        $"{Names(processes)} already {Verb(processes.Count, "holds", "hold")} {held} on " +
                        $"{resource.QualifiedDisplayName} and {Verb(processes.Count, "is", "are")} waiting to convert it to " +
                        $"{wanted}.  {explanation}",
                    Processes = processes,
                    Resources = new[] { resource }
                };
            }
        }

        /// <summary>
        /// Two processes each holding what the other is waiting for - the textbook deadlock, and the
        /// one where naming both objects in order is most of the fix.
        /// </summary>
        private static IEnumerable<DeadlockFinding> OppositeAccessOrder(DeadlockGraph graph)
        {
            // A parallel deadlock is one statement's workers queueing on each other's exchanges.
            // There is no second code path, and no object order to settle on.
            if (graph.IsParallel) yield break;

            var reported = new HashSet<string>();

            foreach (var first in graph.Processes)
            {
                foreach (var second in graph.Processes)
                {
                    if (ReferenceEquals(first, second)) continue;

                    var firstWants = WaitedOn(graph, first).FirstOrDefault(r => Owns(r, second));
                    var secondWants = WaitedOn(graph, second).FirstOrDefault(r => Owns(r, first));

                    if (firstWants is null || secondWants is null) continue;
                    if (ReferenceEquals(firstWants, secondWants)) continue; // A conversion, covered above
                    if (IsSameObjectDifferentIndex(firstWants, secondWants)) continue; // A key lookup, covered above

                    // One finding per pair, whichever way round the loop reaches it.
                    var key = string.Join("|", new[] { first.DisplayName, second.DisplayName }.OrderBy(n => n));
                    if (!reported.Add(key)) continue;

                    yield return new DeadlockFinding
                    {
                        Severity = DeadlockFindingSeverity.Advice,
                        Title = "Objects locked in opposite order",
                        Detail =
                            $"{first.DisplayName} holds {secondWants.QualifiedDisplayName} and is waiting for " +
                            $"{firstWants.QualifiedDisplayName}; {second.DisplayName} holds " +
                            $"{firstWants.QualifiedDisplayName} and is waiting for " +
                            $"{secondWants.QualifiedDisplayName}.  Two code paths that touch both objects in " +
                            "different orders deadlock as soon as they overlap - settle on one order and use it " +
                            "everywhere that touches both.",
                        Processes = new[] { first, second },
                        Resources = new[] { firstWants, secondWants }
                    };
                }
            }
        }

        /// <summary>
        /// Two processes deadlocked over two indexes of one table - the key-lookup (bookmark lookup)
        /// deadlock.  It looks like objects locked in opposite order, but the two objects are indexes
        /// of the same table, so the generic advice to settle on one lock order does not apply: the
        /// engine chooses the order it walks a table's indexes in, not the application.  The fix is a
        /// covering index that removes the lookup, which is worth saying rather than sending the reader
        /// off to reorder something they cannot.
        /// </summary>
        private static IEnumerable<DeadlockFinding> KeyLookupDeadlocks(DeadlockGraph graph)
        {
            if (graph.IsParallel) yield break;

            var reported = new HashSet<string>();

            foreach (var first in graph.Processes)
            {
                foreach (var second in graph.Processes)
                {
                    if (ReferenceEquals(first, second)) continue;

                    var firstWants = WaitedOn(graph, first).FirstOrDefault(r => Owns(r, second));
                    var secondWants = WaitedOn(graph, second).FirstOrDefault(r => Owns(r, first));

                    if (firstWants is null || secondWants is null) continue;
                    if (ReferenceEquals(firstWants, secondWants)) continue; // A conversion, covered elsewhere
                    if (!IsSameObjectDifferentIndex(firstWants, secondWants)) continue;

                    // One finding per pair, whichever way round the loop reaches it.
                    var key = string.Join("|", new[] { first.DisplayName, second.DisplayName }.OrderBy(n => n));
                    if (!reported.Add(key)) continue;

                    yield return new DeadlockFinding
                    {
                        Severity = DeadlockFindingSeverity.Advice,
                        Title = "Key lookup deadlock",
                        Detail =
                            $"{first.DisplayName} and {second.DisplayName} deadlocked over two indexes of " +
                            $"{firstWants.ObjectName} - {IndexList(firstWants, secondWants)}.  This is the classic " +
                            "key-lookup deadlock: one side reads through a nonclustered index and looks the row up in " +
                            "the clustered index, while the other writes the clustered index and then the nonclustered " +
                            "one, so the two meet the same rows in opposite order.  There is no lock order to settle " +
                            "on here - the engine, not the application, decides the order a table's own indexes are " +
                            "touched in - so the fix is to remove the lookup: a covering index, with the columns the " +
                            "reader needs added by INCLUDE, means it never has to visit the second index.",
                        Processes = new[] { first, second },
                        Resources = new[] { firstWants, secondWants }
                    };
                }
            }
        }

        /// <summary>
        /// A cycle through three or more processes.  The pairwise rule above cannot see these - no two
        /// of them hold what the other wants - and following the chain by eye across a graph is
        /// exactly the work worth doing for the reader.
        /// </summary>
        private static IEnumerable<DeadlockFinding> LongerCycle(DeadlockGraph graph)
        {
            if (graph.IsParallel) yield break;

            var cycle = DeadlockCycle.Find(graph);
            if (cycle.Count < 3) yield break; // Two is the pairwise case, already described

            var chain = string.Join(" -> ", cycle
                .Select(step => $"{step.Waiter.DisplayName} waits for {step.Resource.QualifiedDisplayName}")
                .Concat(new[] { cycle[0].Waiter.DisplayName }));

            yield return new DeadlockFinding
            {
                Severity = DeadlockFindingSeverity.Advice,
                Title = $"A cycle of {cycle.Count} processes",
                Detail =
                    $"{chain}.  No two of these hold what the other one wants, so no single pair looks wrong - " +
                    "the deadlock only exists all the way round.  Breaking any one link in the chain is enough, so " +
                    "the cheapest step to reorder or shorten is the one to change.",
                Processes = cycle.Select(step => step.Waiter).Distinct().ToList(),
                Resources = cycle.Select(step => step.Resource).Distinct().ToList()
            };
        }

        /// <summary>
        /// An intra-query deadlock, where one statement's parallel workers deadlock with each other.
        /// Worth calling out because none of the usual advice applies: there is no second application
        /// to reorder.
        /// </summary>
        private static IEnumerable<DeadlockFinding> ParallelDeadlock(DeadlockGraph graph)
        {
            if (!graph.IsParallel) yield break;

            var spid = graph.Processes.FirstOrDefault(p => p.Spid.HasValue)?.Spid;

            yield return new DeadlockFinding
            {
                Severity = DeadlockFindingSeverity.Advice,
                Title = "Parallel (intra-query) deadlock",
                Detail =
                    $"The processes here are parallel workers of one statement{(spid is null ? string.Empty : $" on SPID {spid}")}, " +
                    "not separate sessions - it deadlocked with itself.  Nothing about the application's locking " +
                    "order is at fault, so the fix is on the plan: an index that removes the exchange, or MAXDOP 1 " +
                    "on the statement, is what usually settles it.",
                Processes = graph.Processes.ToList(),
                Resources = graph.Resources.Where(r => r.IsParallelismResource).ToList()
            };
        }

        /// <summary>
        /// The same module on both sides, which changes where to look: the ordering problem is inside
        /// one piece of code rather than between two.
        /// </summary>
        private static IEnumerable<DeadlockFinding> SameModuleBothSides(DeadlockGraph graph)
        {
            if (graph.IsParallel) yield break; // Same statement by definition; already said above

            var byModule = graph.Processes
                .Where(p => p.PrimaryFrame is { IsModule: true })
                .GroupBy(p => p.PrimaryFrame!.ProcedureName!, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);

            foreach (var group in byModule)
            {
                yield return new DeadlockFinding
                {
                    Severity = DeadlockFindingSeverity.Information,
                    Title = "Both sides are running the same module",
                    Detail =
                        $"{Names(group.ToList())} were in {group.Key} when they deadlocked.  Two copies of one " +
                        "procedure deadlocking means the order it takes locks in depends on its parameters - the " +
                        "rows it happens to touch - rather than on another piece of code.",
                    Processes = group.ToList()
                };
            }
        }

        /// <summary>
        /// A writer blocked by a process that only holds shared locks and has written nothing.  This
        /// is the reader/writer deadlock that row versioning removes outright, which is worth knowing
        /// before rewriting any application code.
        /// </summary>
        private static IEnumerable<DeadlockFinding> BlockedByReader(DeadlockGraph graph)
        {
            foreach (var resource in graph.Resources)
            {
                var readerOwners = resource.Owners
                    .Where(o => o.Process is not null && IsShared(o.Mode) && o.Process.LogUsed is null or 0)
                    .ToList();
                var readers = readerOwners.Select(o => o.Process!).ToList();

                var writers = resource.Waiters
                    .Where(w => w.Process is not null && !IsShared(w.Mode))
                    .Select(w => w.Process!)
                    .ToList();

                if (readers.Count == 0 || writers.Count == 0) continue;

                // A RangeS-S lock only exists under serializable, so the isolation level is what to look
                // at here rather than row versioning.
                var rangeLocked = readerOwners.Any(o => IsRangeLock(o.Mode));

                var remedy = rangeLocked
                    ? "The shared lock here is a range lock, so the reader is running under serializable.  Confirm " +
                      "that was intended - if not, drop to an isolation level that does not hold shared range locks."
                    : "Read committed snapshot isolation takes readers out of this picture altogether - they stop " +
                      "taking the shared locks that are half of this deadlock - at the cost of tempdb version store " +
                      "traffic.";

                yield return new DeadlockFinding
                {
                    Severity = DeadlockFindingSeverity.Advice,
                    Title = "A reader is holding up a writer",
                    Detail =
                        $"{Names(readers)} {Verb(readers.Count, "holds", "hold")} a shared lock on " +
                        $"{resource.QualifiedDisplayName} and {Verb(readers.Count, "has", "have")} written nothing, while " +
                        $"{Names(writers)} {Verb(writers.Count, "waits", "wait")} for it.  {remedy}",
                    Processes = readers.Concat(writers).ToList(),
                    Resources = new[] { resource }
                };
            }
        }

        // ---------------------------------------------------------------- settings and shapes

        private static IEnumerable<DeadlockFinding> IsolationLevels(DeadlockGraph graph)
        {
            var strict = graph.Processes
                .Where(p => Mentions(p.IsolationLevel, "serializable") || Mentions(p.IsolationLevel, "repeatable read"))
                .ToList();

            if (strict.Count == 0) yield break;

            yield return new DeadlockFinding
            {
                Severity = DeadlockFindingSeverity.Advice,
                Title = "Running under a stricter isolation level",
                Detail =
                    $"{Names(strict)} ran under {string.Join(", ", strict.Select(p => p.IsolationLevel).Distinct())}.  " +
                    "Shared locks are then held to the end of the transaction rather than released after each " +
                    "statement, and serializable adds range locks on top, so the window for a deadlock is as long as " +
                    "the transaction.  Worth checking the level was asked for deliberately - it is often inherited " +
                    "from a client library default or a leftover SET.",
                Processes = strict
            };
        }

        private static IEnumerable<DeadlockFinding> TableLevelLocks(DeadlockGraph graph)
        {
            var tables = graph.Resources
                .Where(r => r.Type == DeadlockResourceType.ObjectLock && !IsIntent(r.Mode))
                .ToList();

            if (tables.Count == 0) yield break;

            yield return new DeadlockFinding
            {
                Severity = DeadlockFindingSeverity.Advice,
                Title = tables.Count == 1 ? "A whole table is locked" : "Whole tables are locked",
                Detail =
                    $"{string.Join(", ", tables.Select(t => t.ObjectDisplayName))} " +
                    $"{Verb(tables.Count, "is", "are")} locked at table level rather than by " +
                    "row or page.  That is usually lock escalation after a statement touched enough rows, or a " +
                    "TABLOCK hint.  Smaller batches, or an index that lets the statement touch fewer rows, keep the " +
                    "locks narrow enough not to collide.",
                Resources = tables
            };
        }

        private static IEnumerable<DeadlockFinding> HeapLocks(DeadlockGraph graph)
        {
            var heaps = graph.Resources.Where(r => r.Type == DeadlockResourceType.RidLock).ToList();
            if (heaps.Count == 0) yield break;

            yield return new DeadlockFinding
            {
                Severity = DeadlockFindingSeverity.Information,
                Title = "A heap is involved",
                Detail =
                    $"The lock on {string.Join(", ", heaps.Select(h => h.ObjectDisplayName).Distinct())} is a row identifier lock, " +
                    "which only happens on a table with no clustered index.  Statements have to find rows by " +
                    "scanning or through a lookup, so they hold locks on more rows, and for longer, than they would " +
                    "on a clustered table.",
                Resources = heaps
            };
        }

        private static IEnumerable<DeadlockFinding> ApplicationLocks(DeadlockGraph graph)
        {
            var locks = graph.Resources.Where(r => r.Type == DeadlockResourceType.ApplicationLock).ToList();
            if (locks.Count == 0) yield break;

            yield return new DeadlockFinding
            {
                Severity = DeadlockFindingSeverity.Information,
                Title = "Application locks are involved",
                Detail =
                    "This deadlock is over locks the application took itself with sp_getapplock, not over data.  " +
                    "The order they are acquired in is entirely the application's to fix, and the graph cannot show " +
                    "which resource name each hash belongs to - that has to come from the code taking them.",
                Resources = locks
            };
        }

        // ---------------------------------------------------------------- the victim

        private static IEnumerable<DeadlockFinding> DeadlockPriority(DeadlockGraph graph)
        {
            var set = graph.Processes.Where(p => p.Priority is not null and not 0).ToList();
            if (set.Count == 0) yield break;

            // Priority decides the victim only when the processes differ on it; on a tie the engine
            // still falls back to least log to roll back, so claiming priority chose the victim when
            // every process shares one would be wrong.
            var decisive = graph.Processes.Select(p => p.Priority ?? 0).Distinct().Count() > 1;

            var note = decisive
                ? "That is why the engine chose the victim it did, rather than the transaction with least to roll " +
                  "back - worth knowing before reading anything into which side lost."
                : "Every process here ran at the same priority, so this did not decide the victim - the engine still " +
                  "chose the transaction with least to roll back - but it is worth knowing the setting is in play.";

            yield return new DeadlockFinding
            {
                Severity = DeadlockFindingSeverity.Information,
                Title = "Deadlock priority was set",
                Detail =
                    $"{string.Join("; ", set.Select(p => $"{p.DisplayName} ran at DEADLOCK_PRIORITY {p.Priority}"))}.  " +
                    note,
                Processes = set
            };
        }

        private static IEnumerable<DeadlockFinding> VictimCost(DeadlockGraph graph)
        {
            foreach (var victim in graph.Victims.Where(v => v.LogUsed >= NotableLogUsed))
            {
                yield return new DeadlockFinding
                {
                    Severity = DeadlockFindingSeverity.Information,
                    Title = "The victim had a lot to roll back",
                    Detail =
                        $"{victim.DisplayName} had written {DeadlockFormat.Bytes(victim.LogUsed!.Value)} of " +
                        "transaction log when it was chosen as the victim, all of which had to be rolled back.  " +
                        "Long transactions are both more likely to be picked and more expensive when they are.",
                    Processes = new[] { victim }
                };
            }
        }

        // ---------------------------------------------------------------- the capture itself

        private static IEnumerable<DeadlockFinding> CaptureProblems(DeadlockGraph graph)
        {
            var unresolved = graph.Resources
                .SelectMany(r => r.Owners.Concat(r.Waiters))
                .Where(p => p.Process is null)
                .Select(p => p.ProcessId)
                .Distinct()
                .ToList();

            if (unresolved.Count > 0)
            {
                yield return new DeadlockFinding
                {
                    Severity = DeadlockFindingSeverity.Warning,
                    Title = "The capture is incomplete",
                    Detail =
                        $"{unresolved.Count} process{(unresolved.Count == 1 ? " is" : "es are")} referenced by the " +
                        "resources but missing from the process list, so part of the deadlock is not here.  This is " +
                        "usually the system_health ring buffer having dropped it - what is shown is still true, but " +
                        "it is not the whole cycle."
                };
            }

            // A real deadlock always has a cycle.  Not finding one means the graph is missing part of
            // it, which is worth saying plainly: the rest of the findings only describe what is here.
            if (graph.Processes.Count > 1 && DeadlockCycle.Find(graph).Count == 0)
            {
                yield return new DeadlockFinding
                {
                    Severity = DeadlockFindingSeverity.Warning,
                    Title = "No complete cycle in the graph",
                    Detail =
                        "The waits in this graph do not close into a cycle, and a deadlock always does.  Part of it " +
                        "is missing from the capture - a process it went through, or a resource - so treat the rest " +
                        "of what is shown as one side of the story."
                };
            }

            if (graph.Victims.Count == 0)
            {
                yield return new DeadlockFinding
                {
                    Severity = DeadlockFindingSeverity.Warning,
                    Title = "No victim is named",
                    Detail =
                        "The graph does not say which process was rolled back.  Nothing else in it is affected, but " +
                        "the usual starting point - what the victim was doing - is missing."
                };
            }
            else if (graph.Victims.Count > 1)
            {
                yield return new DeadlockFinding
                {
                    Severity = DeadlockFindingSeverity.Information,
                    Title = "More than one victim",
                    Detail =
                        $"{Names(graph.Victims)} were all rolled back.  That happens with a parallel deadlock, where " +
                        "the workers of one statement go together.",
                    Processes = graph.Victims.ToList()
                };
            }
        }

        // ---------------------------------------------------------------- helpers

        private static IEnumerable<DeadlockResource> WaitedOn(DeadlockGraph graph, DeadlockProcess process) =>
            graph.Resources.Where(r => r.Waiters.Any(w => ReferenceEquals(w.Process, process)));

        private static bool Owns(DeadlockResource resource, DeadlockProcess process) =>
            resource.Owners.Any(o => ReferenceEquals(o.Process, process));

        /// <summary>
        /// True when two resources are two different indexes of one table - the shape of a key-lookup
        /// deadlock, where a covering index rather than a lock order is the fix.
        /// </summary>
        private static bool IsSameObjectDifferentIndex(DeadlockResource a, DeadlockResource b) =>
            a.Type == DeadlockResourceType.KeyLock &&
            b.Type == DeadlockResourceType.KeyLock &&
            !string.IsNullOrWhiteSpace(a.ObjectName) &&
            string.Equals(a.ObjectName, b.ObjectName, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(a.IndexName) &&
            !string.IsNullOrWhiteSpace(b.IndexName) &&
            !string.Equals(a.IndexName, b.IndexName, StringComparison.OrdinalIgnoreCase);

        /// <summary>The distinct index names of two resources, joined for display.</summary>
        private static string IndexList(DeadlockResource a, DeadlockResource b) =>
            string.Join(" and ", new[] { a.IndexName, b.IndexName }
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// True for the modes that only conflict with writers.  Intent locks are left out: they say
        /// something is locked further down, not that this process is only reading.
        /// </summary>
        private static bool IsShared(string? mode) =>
            mode is not null &&
            (mode.Equals("S", StringComparison.OrdinalIgnoreCase) ||
             mode.Equals("RangeS-S", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// True for the shared and update modes a read-then-write conversion starts from, so the
        /// conversion advice is only given the read-then-write wording when it fits.
        /// </summary>
        private static bool IsSharedOrUpdate(string? mode) =>
            IsShared(mode) ||
            (mode is not null &&
             (mode.Equals("U", StringComparison.OrdinalIgnoreCase) ||
              mode.Equals("RangeS-U", StringComparison.OrdinalIgnoreCase)));

        /// <summary>
        /// True for range locks, which only arise under serializable.  Their presence rules out the
        /// read committed snapshot isolation remedy, since that only alters read committed.
        /// </summary>
        private static bool IsRangeLock(string? mode) =>
            mode is not null && mode.StartsWith("Range", StringComparison.OrdinalIgnoreCase);

        /// <summary>The granularity word for a lock resource, for the conversion narrative.</summary>
        private static string RowOrResource(DeadlockResource resource) =>
            resource.Type switch
            {
                DeadlockResourceType.KeyLock => "row",
                DeadlockResourceType.RidLock => "row",
                DeadlockResourceType.PageLock => "page",
                _ => "resource"
            };

        private static bool IsIntent(string? mode) =>
            mode is not null && mode.StartsWith("I", StringComparison.OrdinalIgnoreCase);

        private static bool Mentions(string? value, string term) =>
            value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);

        private static string Modes(IEnumerable<DeadlockResourceParticipant> participants)
        {
            var modes = participants
                .Select(p => p.Mode)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return modes.Count == 0 ? "a lock" : string.Join("/", modes);
        }

        /// <summary>Agreement helper, so a finding about one process does not read as a bad translation.</summary>
        private static string Verb(int count, string singular, string plural) => count == 1 ? singular : plural;

        private static string Names(IReadOnlyList<DeadlockProcess> processes) =>
            processes.Count switch
            {
                0 => "No process",
                1 => processes[0].DisplayName,
                _ => string.Join(" and ", new[]
                {
                    string.Join(", ", processes.Take(processes.Count - 1).Select(p => p.DisplayName)),
                    processes[^1].DisplayName
                })
            };
    }
}
