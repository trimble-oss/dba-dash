using DBADash;
using DBADash.Messaging;
using DBADashGUI.CustomReports;
using Microsoft.SqlServer.Management.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADashGUI
{
    public class DBADashContext : ICloneable
    {
        public HashSet<int> InstanceIDs => new HashSet<int>(RegularInstanceIDs.Union(AzureInstanceIDs));
        public HashSet<int> AzureInstanceIDs => ShowHiddenInstances ? AzureInstanceIDsWithHidden : new HashSet<int>(AzureInstanceIDsWithHidden.Except(HiddenInstanceIDs));
        public HashSet<int> RegularInstanceIDs => ShowHiddenInstances ? RegularInstanceIDsWithHidden : new HashSet<int>(RegularInstanceIDsWithHidden.Except(HiddenInstanceIDs));

        public bool ShowHiddenInstances => Common.ShowHidden || InstanceID > 0;

        public HashSet<int> RegularInstanceIDsWithHidden = new();
        public HashSet<int> AzureInstanceIDsWithHidden = new();

        public static HashSet<int> HiddenInstanceIDs = new();

        public string InstanceName { get; set; }
        public string DatabaseName { get; set; }
        public int InstanceID { get; set; }
        public int DatabaseID { get; set; }
        public long ObjectID { get; set; }
        public string ObjectName { get; set; }
        public string SchemaName { get; set; }

        public int TreeLevel { get; set; }

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
        private Version _productVersion;
        private string _connectionID;
        private bool? _hasInstanceMetadata;
        private DatabaseEngineEdition? _engineEdition;

        public bool? HasInstanceMetadata
        {
            get
            {
                if (_hasInstanceMetadata != null) return _hasInstanceMetadata;
                _hasInstanceMetadata = InstanceID >0 ? CommonData.HasInstanceMetadata.Contains(InstanceID) : InstanceIDs.Any(id => CommonData.HasInstanceMetadata.Contains(id));
                return _hasInstanceMetadata;
            }
        }

        public Version ProductVersion
        {
            get
            {
                if (_productVersion != null) return _productVersion;
                GetAdditionalInfo();
                return _productVersion;
            }
        }

        public int? ImportAgentID
        {
            get
            {
                if (_importAgentID != null || InstanceID <= 0) return _importAgentID;
                GetAdditionalInfo();
                return _importAgentID;
            }
        }

        public int? CollectAgentID
        {
            get
            {
                if (_collectAgentID != null || InstanceID <= 0) return _collectAgentID;
                GetAdditionalInfo();
                return _collectAgentID;
            }
        }

        public string ConnectionID
        {
            get
            {
                if (_connectionID != null) return _connectionID;
                GetAdditionalInfo();
                return _connectionID;
            }
        }

        public DatabaseEngineEdition EngineEdition
        {
            get
            {
                if (_engineEdition != null) return _engineEdition.Value;
                GetAdditionalInfo();
                return _engineEdition ?? DatabaseEngineEdition.Unknown;
            }
        }

        private bool? _canMessage;

        private void GetAdditionalInfo()
        {
            if (InstanceID <= 0) return;
            var row = CommonData.Instances.Select($"InstanceID={InstanceID}").FirstOrDefault();
            if (row == null) return;
            _collectAgentID = (int)row["CollectAgentID"];
            _importAgentID = (int)row["ImportAgentID"];
            _connectionID = (string)row["ConnectionID"];
            try
            {
                _productVersion = new Version((string)row["ProductVersion"]);
            }
            catch (Exception ex)
            {
                _productVersion = new Version(0, 0);
                Log.Debug(ex, "Error parsing product version");
            }

            try
            { 
                var engineEditionValue = (int)row["EngineEdition"];
            
                if (Enum.IsDefined(typeof(DatabaseEngineEdition), engineEditionValue))
                {
                    _engineEdition = (DatabaseEngineEdition)engineEditionValue;
                }
                else
                {
                    _engineEdition = DatabaseEngineEdition.Unknown;
                    Log.Debug($"Unknown database engine edition: {engineEditionValue}");
                }
            }
            catch (Exception ex)
            {
                _engineEdition = DatabaseEngineEdition.Unknown;
                Log.Debug(ex, "Error retrieving EngineEdition value.");
            }
        }

        public bool CanMessage
        {
            get
            {
                if (_canMessage != null) return (bool)_canMessage;
                try
                {
                    _canMessage = DBADashUser.AllowMessaging && ImportAgent is { MessagingEnabled: true } &&
                                  CollectAgent is { MessagingEnabled: true };
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Error checking messaging");
                    return false;
                }

                return (bool)_canMessage;
            }
        }

        public bool IsScriptAllowed(ProcedureExecutionMessage.CommunityProcs proc) => IsScriptAllowed("dbo", proc.ToString());

        public bool IsScriptAllowed(string schemaName, string procName)
        {
            if (!CanMessage)
                return false;

            if (Enum.TryParse(typeof(ProcedureExecutionMessage.CommunityProcs), procName, true, out _))
            {
                return (DBADashUser.CommunityScripts &&
                    (CollectAgent.IsAllowAllScripts || CollectAgent.AllowedScripts.Contains(procName)));
            }

            return DBADashUser.CustomTools &&
                   CollectAgent.AllowedCustomProcs.Contains(procName) || CollectAgent.AllowedCustomProcs.Contains(schemaName + "." + procName) || CollectAgent.AllowedCustomProcs.Contains(schemaName.SqlQuoteName() + "." + procName.SqlQuoteName());
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