using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock
{
    /// <summary>
    /// Parses SQL Server deadlock graphs into <see cref="DeadlockGraph"/> objects.
    ///
    /// Deliberately tolerant about the shape of the input, because deadlock XML reaches DBA Dash by
    /// several routes that wrap it differently:
    ///
    ///   * A bare &lt;deadlock&gt; element - the extended events xml_deadlock_report payload, and what
    ///     sp_BlitzLock and sp_HealthParser hand back.
    ///   * A &lt;deadlock-list&gt; wrapper containing one or more &lt;deadlock&gt; elements - what SSMS
    ///     writes when you save a .xdl, and the shape of the Profiler/trace deadlock graph event.  In
    ///     this form the victim is an attribute on &lt;deadlock&gt; rather than a victim-list element.
    ///   * An extended events &lt;event&gt; envelope with the graph nested inside a data/value element.
    ///
    /// Anything else containing a &lt;deadlock&gt; element anywhere is handled too, since the wrapper
    /// varies between tools and none of it is load bearing.  Missing attributes yield nulls rather
    /// than exceptions: graphs from the system_health ring buffer are frequently truncated, and a
    /// partial graph is still worth showing.
    /// </summary>
    public static class DeadlockParser
    {
        /// <summary>
        /// Parse every deadlock in <paramref name="xml"/>, in document order.
        /// </summary>
        /// <exception cref="DeadlockParseException">
        /// The input is null, empty, not well formed XML, or contains no deadlock element.
        /// </exception>
        public static IReadOnlyList<DeadlockGraph> Parse(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new DeadlockParseException("The deadlock graph is empty.");
            }

            XDocument doc;
            try
            {
                // XDocument.Parse prohibits DTD processing by default on modern .NET, so external
                // entities in a graph from an untrusted source are not resolved.
                doc = XDocument.Parse(StripLeadingByteOrderMark(xml));
            }
            catch (XmlException ex)
            {
                throw new DeadlockParseException($"The deadlock graph is not well formed XML: {ex.Message}", ex);
            }

            var root = doc.Root ?? throw new DeadlockParseException("The deadlock graph is empty.");

            // A <deadlock> root is the common case; otherwise search the whole document so that any
            // wrapper - deadlock-list, an XE event envelope, something else - is handled the same way.
            var deadlockElements = IsNamed(root, "deadlock")
                ? new[] { root }
                : root.Descendants().Where(e => IsNamed(e, "deadlock")).ToArray();

            if (deadlockElements.Length == 0)
            {
                throw new DeadlockParseException(
                    $"No deadlock element was found.  The root element is '{root.Name.LocalName}'.");
            }

            return deadlockElements.Select(BuildGraph).ToArray();
        }

        /// <summary>
        /// Parse without throwing.  Returns false and an empty list when the input is not a deadlock graph.
        /// </summary>
        public static bool TryParse(string? xml, out IReadOnlyList<DeadlockGraph> graphs)
        {
            try
            {
                graphs = Parse(xml);
                return true;
            }
            catch (DeadlockParseException)
            {
                graphs = Array.Empty<DeadlockGraph>();
                return false;
            }
        }

        /// <summary>
        /// True when <paramref name="xml"/> contains at least one deadlock graph.
        ///
        /// Intended to replace validity checks that test only for a &lt;deadlock&gt; root element, which
        /// reject the &lt;deadlock-list&gt; form that SSMS saves.
        /// </summary>
        public static bool IsDeadlockXml(string? xml) => TryParse(xml, out _);

        private static DeadlockGraph BuildGraph(XElement deadlockElement)
        {
            var graph = new DeadlockGraph { Xml = deadlockElement.ToString() };

            var processes = ChildrenOf(deadlockElement, "process-list", "process")
                .Select(BuildProcess)
                .ToArray();

            graph.Processes = processes;
            graph.IndexProcesses();

            var resourceList = ElementNamed(deadlockElement, "resource-list");
            var resources = resourceList is null
                ? Array.Empty<DeadlockResource>()
                : MergeByLockId(resourceList.Elements().Select(e => BuildResource(e, graph)));

            graph.Resources = resources;
            graph.Victims = ResolveVictims(deadlockElement, graph);
            graph.IsParallel = DetectParallel(processes, resources);

            return graph;
        }

        /// <summary>
        /// Victims are named either by a victim-list (extended events form) or by a victim attribute
        /// on the deadlock element (trace/.xdl form).  Both are read, since a graph can carry either.
        /// Ids naming a process that is not in the process-list are ignored.
        /// </summary>
        private static IReadOnlyList<DeadlockProcess> ResolveVictims(XElement deadlockElement, DeadlockGraph graph)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var victim in ChildrenOf(deadlockElement, "victim-list", "victimProcess"))
            {
                var id = Attr(victim, "id");
                if (!string.IsNullOrEmpty(id)) ids.Add(id);
            }

            var victimAttribute = Attr(deadlockElement, "victim");
            if (!string.IsNullOrEmpty(victimAttribute)) ids.Add(victimAttribute);

            if (ids.Count == 0) return Array.Empty<DeadlockProcess>();

            // Preserve process-list order rather than the order the ids were declared in.
            var victims = graph.Processes.Where(p => ids.Contains(p.Id)).ToArray();
            foreach (var victim in victims)
            {
                victim.IsVictim = true;
            }
            return victims;
        }

        /// <summary>
        /// A parallel (intra-query) deadlock shows up either as a parallelism resource type, or as
        /// several process entries sharing one spid but differing by execution context id.
        /// </summary>
        private static bool DetectParallel(
            IReadOnlyList<DeadlockProcess> processes,
            IReadOnlyList<DeadlockResource> resources)
        {
            if (resources.Any(r => r.IsParallelismResource)) return true;

            return processes
                .Where(p => p.Spid.HasValue)
                .GroupBy(p => p.Spid!.Value)
                .Any(g => g.Count() > 1);
        }

        private static DeadlockProcess BuildProcess(XElement el)
        {
            var process = new DeadlockProcess
            {
                Id = Attr(el, "id") ?? string.Empty,
                Spid = ParseInt(Attr(el, "spid")),
                Ecid = ParseInt(Attr(el, "ecid")),
                Status = Attr(el, "status"),
                LoginName = Attr(el, "loginname"),
                HostName = Attr(el, "hostname"),
                HostPid = ParseInt(Attr(el, "hostpid")),
                ClientApp = Attr(el, "clientapp"),
                IsolationLevel = Attr(el, "isolationlevel"),
                LockMode = Attr(el, "lockMode"),
                WaitResource = Attr(el, "waitresource"),
                TransactionCount = ParseInt(Attr(el, "trancount")),
                TransactionName = Attr(el, "transactionname"),
                LastTransactionStarted = ParseDateTime(Attr(el, "lasttranstarted")),
                LastBatchStarted = ParseDateTime(Attr(el, "lastbatchstarted")),
                LastBatchCompleted = ParseDateTime(Attr(el, "lastbatchcompleted")),
                LogUsed = ParseLong(Attr(el, "logused")),
                Priority = ParseInt(Attr(el, "priority")),
                CurrentDatabaseId = ParseInt(Attr(el, "currentdb")),
                CurrentDatabaseName = Attr(el, "currentdbname"),
                InputBuffer = TrimToNull(ElementNamed(el, "inputbuf")?.Value),
                ExecutionStack = ChildrenOf(el, "executionStack", "frame").Select(BuildFrame).ToArray()
            };

            var waitTimeMs = ParseLong(Attr(el, "waittime"));
            if (waitTimeMs.HasValue)
            {
                process.WaitTime = TimeSpan.FromMilliseconds(waitTimeMs.Value);
            }

            return process;
        }

        private static DeadlockFrame BuildFrame(XElement el) => new()
        {
            ProcedureName = Attr(el, "procname"),
            Line = ParseInt(Attr(el, "line")),
            StatementStart = ParseInt(Attr(el, "stmtstart")),
            StatementEnd = ParseInt(Attr(el, "stmtend")),
            SqlHandle = Attr(el, "sqlhandle"),
            Sql = StatementText(el.Value)
        };

        private static DeadlockResource BuildResource(XElement el, DeadlockGraph graph)
        {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var attribute in el.Attributes())
            {
                attributes[attribute.Name.LocalName] = attribute.Value;
            }

            return new DeadlockResource
            {
                Type = MapResourceType(el.Name.LocalName),
                TypeName = el.Name.LocalName,
                Id = Attr(el, "id"),
                ObjectName = Attr(el, "objectname"),
                IndexName = Attr(el, "indexname"),
                DatabaseId = ParseInt(Attr(el, "dbid")),
                Mode = Attr(el, "mode"),
                HobtId = ParseLong(Attr(el, "hobtid")),
                AssociatedObjectId = ParseLong(Attr(el, "associatedObjectId")),
                FileId = ParseInt(Attr(el, "fileid")),
                PageId = ParseLong(Attr(el, "pageid")),
                Owners = Distinct(BuildParticipants(el, "owner-list", "owner", graph)),
                Waiters = Distinct(BuildParticipants(el, "waiter-list", "waiter", graph)),
                Attributes = attributes
            };
        }

        /// <summary>
        /// One lock, one resource.
        ///
        /// SQL Server writes the resource-list as a list of <em>edges</em> rather than of resources:
        /// a page that four sessions are queued on is written out four times, each entry carrying one
        /// owner and one waiter, all sharing a lock id.  Taken literally that is four identical boxes
        /// on the graph, four rows in the grid, and a picture nobody can read - so entries sharing a
        /// lock id are folded back into the one resource they describe, with the owners and waiters
        /// unioned.
        ///
        /// Only entries carrying a lock id are merged.  Resource types that have none (waitfor, and
        /// some parallelism resources) are left exactly as they were written, since there is nothing
        /// to say two such entries are the same thing.
        /// </summary>
        private static IReadOnlyList<DeadlockResource> MergeByLockId(IEnumerable<DeadlockResource> resources)
        {
            var merged = new List<DeadlockResource>();
            var byKey = new Dictionary<(string Type, string Id), DeadlockResource>();

            foreach (var resource in resources)
            {
                if (string.IsNullOrEmpty(resource.Id))
                {
                    merged.Add(resource);
                    continue;
                }

                // The type is part of the key only as a guard: two different lock types sharing an id
                // would be a graph we do not understand, and merging them would hide that.
                var key = (resource.TypeName.ToLowerInvariant(), resource.Id!);

                if (!byKey.TryGetValue(key, out var existing))
                {
                    byKey.Add(key, resource);
                    merged.Add(resource);
                    continue;
                }

                existing.Owners = Combine(existing.Owners, resource.Owners);
                existing.Waiters = Combine(existing.Waiters, resource.Waiters);
            }

            return merged;
        }

        private static IReadOnlyList<DeadlockResourceParticipant> Combine(
            IReadOnlyList<DeadlockResourceParticipant> first, IReadOnlyList<DeadlockResourceParticipant> second) =>
            Distinct(first.Concat(second));

        /// <summary>
        /// Drops repeated owner and waiter entries - the same process, in the same mode, for the same
        /// reason.  A graph lists a process once per lock owner block it holds, so one process holding
        /// a page can be written two or three times over; each repeat is another arrow on the picture
        /// saying nothing the first one did not.
        /// </summary>
        private static IReadOnlyList<DeadlockResourceParticipant> Distinct(
            IEnumerable<DeadlockResourceParticipant> participants)
        {
            var seen = new HashSet<(string, string, string)>();
            var result = new List<DeadlockResourceParticipant>();

            foreach (var participant in participants)
            {
                var key = (
                    participant.ProcessId.ToLowerInvariant(),
                    participant.Mode?.ToLowerInvariant() ?? string.Empty,
                    participant.RequestType?.ToLowerInvariant() ?? string.Empty);

                if (seen.Add(key)) result.Add(participant);
            }

            return result.ToArray();
        }

        private static IReadOnlyList<DeadlockResourceParticipant> BuildParticipants(
            XElement resource, string listName, string itemName, DeadlockGraph graph)
        {
            return ChildrenOf(resource, listName, itemName)
                .Select(el =>
                {
                    var processId = Attr(el, "id") ?? string.Empty;
                    return new DeadlockResourceParticipant
                    {
                        ProcessId = processId,
                        Process = graph.FindProcess(processId),
                        Mode = Attr(el, "mode"),
                        RequestType = Attr(el, "requestType")
                    };
                })
                .ToArray();
        }

        private static DeadlockResourceType MapResourceType(string elementName) =>
            elementName.ToLowerInvariant() switch
            {
                "keylock" => DeadlockResourceType.KeyLock,
                "pagelock" => DeadlockResourceType.PageLock,
                "objectlock" => DeadlockResourceType.ObjectLock,
                "ridlock" => DeadlockResourceType.RidLock,
                "hobtlock" => DeadlockResourceType.HobtLock,
                "allocunitlock" => DeadlockResourceType.AllocUnitLock,
                "databaselock" => DeadlockResourceType.DatabaseLock,
                "filelock" => DeadlockResourceType.FileLock,
                "extentlock" => DeadlockResourceType.ExtentLock,
                "applicationlock" => DeadlockResourceType.ApplicationLock,
                "metadatalock" => DeadlockResourceType.MetadataLock,
                "xactlock" => DeadlockResourceType.TransactionLock,
                "exchangeevent" => DeadlockResourceType.ExchangeEvent,
                "threadpoolwait" => DeadlockResourceType.ThreadPoolWait,
                "waitfor" => DeadlockResourceType.WaitFor,
                "syncpoint" => DeadlockResourceType.SyncPoint,
                _ => DeadlockResourceType.Unknown
            };

        /// <summary>
        /// Elements matching <paramref name="itemName"/> inside the <paramref name="listName"/> child.
        /// Returns empty when the list element is absent, which is normal for e.g. a resource with no waiters.
        /// </summary>
        private static IEnumerable<XElement> ChildrenOf(XElement parent, string listName, string itemName)
        {
            var list = ElementNamed(parent, listName);
            return list is null ? Enumerable.Empty<XElement>() : ElementsNamed(list, itemName);
        }

        // Element and attribute names are matched on local name and case insensitively.  Deadlock XML
        // carries no namespace, but it can arrive nested inside documents that do, and the casing of
        // names such as exchangeEvent and lockMode has not been consistent across SQL Server versions.
        private static bool IsNamed(XElement element, string localName) =>
            string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase);

        private static IEnumerable<XElement> ElementsNamed(XContainer parent, string localName) =>
            parent.Elements().Where(e => IsNamed(e, localName));

        private static XElement? ElementNamed(XContainer parent, string localName) =>
            ElementsNamed(parent, localName).FirstOrDefault();

        private static string? Attr(XElement element, string localName)
        {
            foreach (var attribute in element.Attributes())
            {
                if (string.Equals(attribute.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))
                {
                    return attribute.Value;
                }
            }
            return null;
        }

        private static int? ParseInt(string? value) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;

        private static long? ParseLong(string? value) =>
            long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;

        /// <summary>
        /// Deadlock timestamps are written without a time zone and represent the source instance's
        /// local time, so they are parsed as <see cref="DateTimeKind.Unspecified"/> and left for the
        /// caller to interpret rather than being silently treated as UTC.
        /// </summary>
        private static DateTime? ParseDateTime(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                ? result
                : null;

        private static string? TrimToNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        /// <summary>
        /// The text of an execution stack frame, or null when SQL Server could not resolve it.  When
        /// the statement text is not available - an adhoc batch whose plan has aged out of cache, for
        /// instance - SQL Server writes the literal placeholder "unknown", which is not a statement
        /// and would otherwise be shown in place of the real one from the input buffer.
        /// </summary>
        private static string? StatementText(string? value)
        {
            var trimmed = TrimToNull(value);
            return string.Equals(trimmed, "unknown", StringComparison.OrdinalIgnoreCase) ? null : trimmed;
        }

        /// <summary>
        /// char.IsWhiteSpace does not treat U+FEFF as whitespace on modern .NET, so a byte order mark
        /// surviving a round trip through a string column would otherwise fail the parse.
        /// </summary>
        private static string StripLeadingByteOrderMark(string xml) =>
            xml.TrimStart('﻿', ' ', '\t', '\r', '\n');
    }
}
