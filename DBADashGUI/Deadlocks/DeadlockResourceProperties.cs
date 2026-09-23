using System;
using System.ComponentModel;
using System.Linq;
using DBADash.Deadlock.Model;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// A read-only PropertyGrid view of a <see cref="DeadlockResource"/> - the lock at the centre of an
    /// edge in the deadlock, with the processes that own it and the ones blocked on it.  Shown in the
    /// docked panel when a resource node is selected on the graph.
    /// </summary>
    public sealed class DeadlockResourceProperties
    {
        private readonly DeadlockResource _resource;

        public DeadlockResourceProperties(DeadlockResource resource)
        {
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
            Owners = _resource.Owners.Select(p => new DeadlockParticipantProperties(p)).ToArray();
            Waiters = _resource.Waiters.Select(p => new DeadlockParticipantProperties(p)).ToArray();
        }

        [Category("Resource"), DisplayName("Type")]
        public string Type => _resource.TypeName;

        [Category("Resource"), DisplayName("Object")]
        public string ObjectName => _resource.ObjectName;

        [Category("Resource"), DisplayName("Index")]
        public string IndexName => _resource.IndexName;

        [Category("Resource"), DisplayName("Database")]
        public string Database => _resource.DatabaseName;

        [Category("Resource"), DisplayName("Database Id")]
        public int? DatabaseId => _resource.DatabaseId;

        [Category("Resource"), DisplayName("Mode")]
        public string Mode => _resource.Mode;

        [Category("Resource"), DisplayName("Page"),
         Description("The page in db:file:page form, as it appears in a process's wait resource and as "
                     + "DBCC PAGE takes it.  Null for resources that are not page based.")]
        public string Page => _resource.PageKey;

        [Category("Resource"), DisplayName("HoBt Id")]
        public long? HobtId => _resource.HobtId;

        [Category("Resource"), DisplayName("Associated Object Id")]
        public long? AssociatedObjectId => _resource.AssociatedObjectId;

        [Category("Participants"), DisplayName("Owners"),
         Description("The processes that hold the resource.")]
        public DeadlockParticipantProperties[] Owners { get; }

        [Category("Participants"), DisplayName("Waiters"),
         Description("The processes blocked waiting on the resource.")]
        public DeadlockParticipantProperties[] Waiters { get; }

        public override string ToString() => _resource.DisplayName;
    }

    /// <summary>
    /// A read-only PropertyGrid view of one owner or waiter on a resource, shown expandable within the
    /// parent resource's Owners / Waiters.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public sealed class DeadlockParticipantProperties
    {
        private readonly DeadlockResourceParticipant _participant;

        public DeadlockParticipantProperties(DeadlockResourceParticipant participant)
        {
            _participant = participant ?? throw new ArgumentNullException(nameof(participant));
        }

        [DisplayName("Process")]
        public string Process => _participant.Process?.DisplayName ?? _participant.ProcessId;

        public string Mode => _participant.Mode;

        [DisplayName("Request Type"),
         Description("Waiters only, e.g. \"wait\" or \"convert\".")]
        public string RequestType => _participant.RequestType;

        public override string ToString()
        {
            var name = _participant.Process?.DisplayName ?? _participant.ProcessId;
            return string.IsNullOrWhiteSpace(_participant.Mode) ? name : $"{name} ({_participant.Mode})";
        }
    }
}
