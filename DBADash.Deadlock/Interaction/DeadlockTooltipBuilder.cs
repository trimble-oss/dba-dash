using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.Deadlock.Layout;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock.Interaction
{
    /// <summary>
    /// Turns a node into the rows shown when hovering it.
    ///
    /// A node on the graph only has room for a handful of lines, so the tooltip is where the rest of
    /// what the graph captured becomes visible without opening another panel.  Rows whose value is
    /// absent are omitted rather than shown blank - deadlock graphs are frequently partial.
    /// </summary>
    public static class DeadlockTooltipBuilder
    {
        /// <summary>Statement text longer than this is collapsed to one line and truncated.</summary>
        public const int DefaultMaxStatementLength = 200;

        /// <summary>
        /// Attributes already surfaced as their own rows, so the catch-all pass does not repeat them.
        /// </summary>
        private static readonly HashSet<string> HandledResourceAttributes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "id", "objectname", "indexname", "mode", "dbid", "hobtid", "associatedObjectId",
                "fileid", "pageid"
            };

        public static DeadlockTooltip Build(DeadlockNode node, int maxStatementLength = DefaultMaxStatementLength)
        {
            ArgumentNullException.ThrowIfNull(node);

            return node switch
            {
                DeadlockProcessNode process => BuildProcess(process.Process, maxStatementLength),
                DeadlockResourceNode resource => BuildResource(resource.Resource),
                _ => new DeadlockTooltip(node.Title, null, Array.Empty<DeadlockTooltipRow>())
            };
        }

        private static DeadlockTooltip BuildProcess(DeadlockProcess process, int maxStatementLength)
        {
            var rows = new List<DeadlockTooltipRow>();

            Add(rows, "Status", process.Status);
            Add(rows, "Login", process.LoginName);
            Add(rows, "Host", FormatHost(process));
            Add(rows, "Application", process.ClientApp);
            Add(rows, "Database", process.CurrentDatabaseName);
            Add(rows, "Isolation level", process.IsolationLevel);
            Add(rows, "Lock mode", process.LockMode);
            Add(rows, "Wait resource", process.WaitResource);

            if (process.WaitTime is { } wait) Add(rows, "Wait time", DeadlockFormat.Duration(wait));

            Add(rows, "Transaction", process.TransactionName);
            if (process.TransactionCount is { } trancount)
            {
                Add(rows, "Transaction count", DeadlockFormat.Number(trancount));
            }

            if (process.LogUsed is { } logUsed) Add(rows, "Log used", $"{DeadlockFormat.Number(logUsed)} bytes");
            if (process.Priority is { } priority) Add(rows, "Deadlock priority", DeadlockFormat.Number(priority));
            if (process.LastTransactionStarted is { } started)
            {
                Add(rows, "Transaction started", FormatTimestamp(started));
            }

            if (!string.IsNullOrWhiteSpace(process.PrimaryStatement))
            {
                Add(rows, "Statement", DeadlockFormat.SingleLine(process.PrimaryStatement!, maxStatementLength));
            }

            return new DeadlockTooltip(
                process.DisplayName,
                process.IsVictim ? "Deadlock victim" : null,
                rows);
        }

        private static DeadlockTooltip BuildResource(DeadlockResource resource)
        {
            var rows = new List<DeadlockTooltipRow>();

            // Split rather than the three part name in one row: the tooltip is where the database
            // belongs - it has the room - but it should not push the table name along with it.
            Add(rows, "Database", resource.DatabaseName);
            Add(rows, "Object", resource.SchemaQualifiedName);
            Add(rows, "Index", resource.IndexName);
            Add(rows, "Mode", resource.Mode);

            // db:file:page, matching the process waitresource attribute, so the process waiting on a
            // page and the page itself can be matched up without arithmetic.
            Add(rows, "Page", resource.PageKey);

            if (resource.DatabaseId is { } dbid) Add(rows, "Database ID", DeadlockFormat.Identifier(dbid));
            if (resource.HobtId is { } hobt) Add(rows, "HoBt ID", DeadlockFormat.Identifier(hobt));
            if (resource.AssociatedObjectId is { } associated)
            {
                Add(rows, "Associated object ID", DeadlockFormat.Identifier(associated));
            }

            // The lock id is what says whether two entries in the raw XML are the same lock - which is
            // exactly the question anyone comparing the graph against the XML tab is asking.
            Add(rows, "Lock ID", resource.Id);

            Add(rows, "Owners", FormatParticipants(resource.Owners));
            Add(rows, "Waiters", FormatParticipants(resource.Waiters));

            // Whatever this library does not model explicitly still shows, so an unrecognised lock
            // type from a future SQL Server release is fully inspectable.
            foreach (var attribute in resource.Attributes.Where(a => !HandledResourceAttributes.Contains(a.Key)))
            {
                Add(rows, attribute.Key, attribute.Value);
            }

            // The title already carries the object name where there is one; otherwise it is the
            // element name and repeating it as a subtitle would be noise.
            var subtitle = string.IsNullOrWhiteSpace(resource.ObjectName) ? null : resource.TypeName;
            return new DeadlockTooltip(resource.DisplayName, subtitle, rows);
        }

        private static string? FormatHost(DeadlockProcess process)
        {
            if (string.IsNullOrWhiteSpace(process.HostName)) return null;
            return process.HostPid is { } pid
                ? $"{process.HostName} ({DeadlockFormat.Identifier(pid)})"
                : process.HostName;
        }

        /// <summary>
        /// Deadlock timestamps have no time zone - they are the source instance's local time - so
        /// they are shown exactly as captured rather than converted to anything.
        /// </summary>
        private static string FormatTimestamp(DateTime value) =>
            value.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);

        private static string? FormatParticipants(IReadOnlyList<DeadlockResourceParticipant> participants)
        {
            if (participants.Count == 0) return null;

            var parts = participants.Select(p =>
            {
                // An unresolved id means the process is missing from the process-list, which happens
                // with truncated graphs.  Showing the raw id beats showing nothing.
                var name = p.Process?.DisplayName ?? p.ProcessId;
                return string.IsNullOrWhiteSpace(p.Mode) ? name : $"{name} ({p.Mode})";
            });

            return string.Join(", ", parts);
        }

        private static void Add(List<DeadlockTooltipRow> rows, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                rows.Add(new DeadlockTooltipRow(label, value!));
            }
        }
    }
}
