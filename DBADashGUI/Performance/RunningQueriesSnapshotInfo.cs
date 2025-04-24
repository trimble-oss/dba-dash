using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.Performance
{
    public class RunningQueriesSnapshotInfo
    {
        public RunningQueriesSnapshotInfo(DateTime snapshotDateUtc, int instanceId, string title)
        {
            SnapshotDateUtc = snapshotDateUtc;
            InstanceID = instanceId;
            Title = title;
        }

        public DateTime SnapshotDateUtc { get; }
        public int InstanceID { get; }

        public string Title { get; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var other = (RunningQueriesSnapshotInfo)obj;
            return (SnapshotDateUtc == other.SnapshotDateUtc) && (InstanceID == other.InstanceID);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SnapshotDateUtc, InstanceID);
        }
    }
}