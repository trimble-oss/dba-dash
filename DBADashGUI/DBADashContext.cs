using System;
using System.Collections.Generic;
using DBADashGUI.CustomReports;

namespace DBADashGUI
{
    public class DBADashContext : ICloneable
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
        public SQLTreeItem.TreeType ParentType { get; set; }

        public int MasterInstanceID { get; set; }

        public string DriveName { get; set; }
        public string ElasticPoolName { get; set; }

        public CustomReport Report { get; set; }

        public object Clone()
        {
            var clone = (DBADashContext)MemberwiseClone();
            return clone;
        }
    }
}