using System;
using System.Collections.Generic;
using System.Linq;
using DBADash;
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

        private int? _importAgentID;
        private int? _collectAgentID;

        public int? ImportAgentID
        {
            get
            {
                if(_importAgentID != null || InstanceID <=0) return _importAgentID;
                GetInstanceAgentInfo();
                return _importAgentID;
            }
        }

        public int? CollectAgentID
        {
            get
            {
                if(_collectAgentID != null || InstanceID<=0) return _collectAgentID;
                GetInstanceAgentInfo();
                return _collectAgentID;
            }
        }

        private bool? _canMessage;

        private void GetInstanceAgentInfo()
        {
            if(InstanceID<=0) return;
            var row =  CommonData.Instances.Select($"InstanceID={InstanceID}").FirstOrDefault();
            if (row == null) return;
            _collectAgentID = (int)row["CollectAgentID"];
            _importAgentID = (int)row["ImportAgentID"];
        }

        public bool CanMessage
        {
            get
            {
                if (_canMessage != null) return (bool)_canMessage;
                _canMessage= DBADashUser.AllowMessaging && ImportAgent is { MessagingEnabled: true } && CollectAgent is {MessagingEnabled:true};
                return (bool)_canMessage;
            }
        }

        public DBADashAgent ImportAgent => ImportAgentID == null ? null : CommonData.GetDBADashAgent((int)ImportAgentID);

        public DBADashAgent CollectAgent => CollectAgentID == null ? null : CommonData.GetDBADashAgent((int)CollectAgentID);

        public object Clone()
        {
            var clone = (DBADashContext)MemberwiseClone();
            clone._canMessage = null;
            return clone;
        }
    }
}