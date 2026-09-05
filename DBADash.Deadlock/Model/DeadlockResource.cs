using System;
using System.Collections.Generic;

namespace DBADash.Deadlock.Model
{
    /// <summary>
    /// A resource involved in the deadlock, from an entry in the resource-list.  The owners hold it
    /// and the waiters are blocked on it; the cycle those edges form is the deadlock.
    /// </summary>
    public sealed class DeadlockResource
    {
        public DeadlockResourceType Type { get; internal set; }

        /// <summary>The raw element name from the graph, e.g. "keylock", "exchangeEvent".</summary>
        public string TypeName { get; internal set; } = string.Empty;

        /// <summary>Graph-internal lock id, e.g. "lock1e0e8c8c58".  Not present on every resource type.</summary>
        public string? Id { get; internal set; }

        /// <summary>Three part object name where the graph supplies one, e.g. "mydb.dbo.MyTable".</summary>
        public string? ObjectName { get; internal set; }

        public string? IndexName { get; internal set; }

        public int? DatabaseId { get; internal set; }

        /// <summary>The mode the resource is held in, e.g. "X", "U", "RangeS-U".</summary>
        public string? Mode { get; internal set; }

        public long? HobtId { get; internal set; }

        public long? AssociatedObjectId { get; internal set; }

        /// <summary>The data file, on the page based lock types.  Null on the others.</summary>
        public int? FileId { get; internal set; }

        /// <summary>The page, on the page based lock types.  Null on the others.</summary>
        public long? PageId { get; internal set; }

        /// <summary>
        /// The page this lock is on, written the way the process's waitresource attribute writes it -
        /// db:file:page, e.g. "13:1:57567584" - so the two can be matched by eye, and so the value can
        /// be pasted straight into DBCC PAGE.  Null for resources that are not page based.
        ///
        /// Worth having as its own thing because it is frequently the <em>only</em> thing telling two
        /// resources apart: a parallel DELETE deadlocking over one table produces a dozen page locks
        /// whose object name, index, database and hobt id are all identical.
        /// </summary>
        public string? PageKey =>
            PageId is null
                ? null
                : DatabaseId is { } dbid
                    ? $"{dbid}:{FileId ?? 0}:{PageId}"
                    : $"{FileId ?? 0}:{PageId}";

        public IReadOnlyList<DeadlockResourceParticipant> Owners { get; internal set; }
            = Array.Empty<DeadlockResourceParticipant>();

        public IReadOnlyList<DeadlockResourceParticipant> Waiters { get; internal set; }
            = Array.Empty<DeadlockResourceParticipant>();

        /// <summary>
        /// Every attribute present on the resource element, keyed by attribute name.  The typed
        /// properties above cover the common ones; this keeps the rest available so that resource
        /// types this library does not model explicitly can still be shown in full.
        /// </summary>
        public IReadOnlyDictionary<string, string> Attributes { get; internal set; }
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The database part of <see cref="ObjectName"/> - "Sales" of "Sales.dbo.Orders" - or null
        /// when the graph gave no object name, or one that is not database qualified.
        /// </summary>
        public string? DatabaseName => SplitObjectName().Database;

        /// <summary>
        /// <see cref="ObjectName"/> without the database in front of it - "dbo.Orders" - or the whole
        /// name when there is no database part to remove.  Null when there is no object name.
        ///
        /// This is what identifies the object on screen.  A deadlock is nearly always inside one
        /// database, so repeating its name on every box says nothing - and when the database is named
        /// after a tenant GUID it is long enough to push the table name out of the box entirely.
        /// </summary>
        public string? SchemaQualifiedName => SplitObjectName().Object;

        /// <summary>
        /// Splits <see cref="ObjectName"/> at the last two dots rather than the first.  Schema and
        /// object names very rarely contain a dot and database names sometimes do, and the graph
        /// writes the name unquoted, so there is nothing else to go on.
        /// </summary>
        private (string? Database, string? Object) SplitObjectName()
        {
            if (string.IsNullOrWhiteSpace(ObjectName)) return (null, null);

            var parts = ObjectName!.Split('.');
            if (parts.Length < 3) return (null, ObjectName);

            return (string.Join('.', parts[..^2]), $"{parts[^2]}.{parts[^1]}");
        }

        /// <summary>
        /// The object this resource belongs to, database and all - the three part name, with the index
        /// where there is one, falling back to the element name.  Prose about the object uses this:
        /// a finding naming a table is read away from the graph, where nothing else says which
        /// database it was in.  What <see cref="DisplayName"/> was before page based locks
        /// started naming themselves by their page, and what prose about the <em>table</em> wants:
        /// "the lock on Page 4:1:884 is a row identifier lock" says nothing about which table has no
        /// clustered index.
        /// </summary>
        public string ObjectDisplayName =>
            string.IsNullOrWhiteSpace(ObjectName)
                ? TypeName
                : string.IsNullOrWhiteSpace(IndexName)
                    ? ObjectName!
                    : $"{ObjectName} ({IndexName})";

        /// <summary>
        /// Short identifier for display - the schema qualified object name (with index where present),
        /// falling back to the raw element name for resources that are not object based, such as
        /// exchangeEvent.  The database is left off - see <see cref="SchemaQualifiedName"/> - and shown
        /// separately by anything with room for it.
        ///
        /// Page based locks are named by their page instead, because the object name does not tell
        /// them apart: every page lock in a graph can carry the same table name.  The object name is
        /// still shown, just not as the thing that identifies the resource.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (PageKey is { } page)
                {
                    // A RID lock is a row on a page, so the page alone would not identify it.
                    return Type == DeadlockResourceType.RidLock && Attributes.TryGetValue("rid", out var rid)
                        ? $"RID {page}:{rid}"
                        : $"Page {page}";
                }

                if (string.IsNullOrWhiteSpace(ObjectName)) return TypeName;

                return string.IsNullOrWhiteSpace(IndexName)
                    ? SchemaQualifiedName!
                    : $"{SchemaQualifiedName} ({IndexName})";
            }
        }

        /// <summary>
        /// The same identity as <see cref="DisplayName"/>, with the database put back when the
        /// identity is an object name.  For prose - a finding is copied into a ticket and read away
        /// from the graph, where nothing else says which database the table was in, so it has to
        /// carry its own context.  The picture uses <see cref="DisplayName"/>, which has no room for
        /// a database name it would be repeating on every box.
        /// </summary>
        public string QualifiedDisplayName => PageKey is null ? ObjectDisplayName : DisplayName;

        /// <summary>
        /// True for resource types that only arise within a single parallel query rather than
        /// between sessions.
        /// </summary>
        public bool IsParallelismResource =>
            Type is DeadlockResourceType.ExchangeEvent
                or DeadlockResourceType.ThreadPoolWait
                or DeadlockResourceType.SyncPoint;
    }
}
