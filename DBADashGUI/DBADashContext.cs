using System;
using System.Collections.Generic;

namespace DBADashGUI
{
    public class DBADashContext
    {
        public HashSet<int> InstanceIDs;
        public HashSet<int> AzureInstanceIDs;
        public HashSet<int> RegularInstanceIDs;
        public string InstanceName { get; set; }
        public string DatabaseName { get; set; }
        public int InstanceID { get; set; }
        public int DatabaseID { get; set; }
        public long ObjectID { get; set; }
        public string ObjectName { get; set; }

        public Guid JobID { get; set; }
        public int JobStepID { get; set; }

        public SQLTreeItem.TreeType Type { get; set; }

    }
}
