using DBADash;
using DBADash.Messaging;
using DBADashGUI.CommunityTools;
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
        private bool? _hasResourceGovernorWorkloadGroups;

        public bool? HasInstanceMetadata
        {
            get
            {
                if (_hasInstanceMetadata != null) return _hasInstanceMetadata;
                _hasInstanceMetadata = InstanceID > 0 ? CommonData.HasInstanceMetadata.Contains(InstanceID) : InstanceIDs.Any(id => CommonData.HasInstanceMetadata.Contains(id));
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

        public bool HasResourceGovernorWorkloadGroups
        {
            get
            {
                if (_hasResourceGovernorWorkloadGroups != null) return _hasResourceGovernorWorkloadGroups.Value;
                GetAdditionalInfo();
                return _hasResourceGovernorWorkloadGroups.Value;
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
            _hasResourceGovernorWorkloadGroups = (bool)row["HasResourceGovernorWorkloadGroups"];
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

        public bool IsAzure => EngineEdition == DatabaseEngineEdition.SqlDatabase || EngineEdition == DatabaseEngineEdition.SqlManagedInstance || EngineEdition == DatabaseEngineEdition.SqlAzureArcManagedInstance;

        public bool InstanceSupportsQueryStore => ProductVersion != null && (ProductVersion.Major >= 13 || IsAzure);

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

        public bool IsReportAllowed(DirectExecutionReport rpt)
        {
            if (rpt is SystemDirectExecutionReport srpt)
            {
                return DBADashUser.AllowMessaging;
            }
            return IsScriptAllowed(rpt.SchemaName, rpt.ProcedureName);
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

        /// <summary>
        /// Check if the instance's product version is greater than or equal to the specified minimum version.  Version check always succeeds for Azure DB and managed instance.
        /// </summary>
        /// <param name="minVersion"></param>
        /// <returns>True if version is greater than or equal to minVersion</returns>
        public bool IsMinimumCompatibleVersion(Version minVersion) => ProductVersion?.CompareTo(minVersion) >= 0 || IsAzure;

        /// <summary>
        /// Check if the instance's product version is greater than or equal to the specified minimum version.  Version check always succeeds for Azure DB and managed instance.
        /// </summary>
        /// <param name="minVersion"></param>
        /// <returns>True if version is greater than or equal to minVersion</returns>
        public bool IsMinimumCompatibleVersion(int major, int minor = 0, int build = 0)
        {
            var minVersion = new Version(major, minor, build);
            return IsMinimumCompatibleVersion(minVersion);
        }

        /// <summary>
        /// Checks version and database engine edition to determine if Query Tuning Recommendations are supported
        /// </summary>
        /// <returns>True if sys.dm_db_tuning_recommendations is available engine edition supports it</returns>
        public bool IsQueryTuningRecommendationsSupported() => IsMinimumCompatibleVersion(14) && EngineEdition is DatabaseEngineEdition.Enterprise or DatabaseEngineEdition.SqlManagedInstance or DatabaseEngineEdition.SqlAzureArcManagedInstance or DatabaseEngineEdition.SqlDatabase;

        /// <summary>
        /// Checks version to determine if Query Store is supported
        /// </summary>
        /// <returns>True if query store is supported</returns>
        public bool IsQueryStoreSupported() => IsMinimumCompatibleVersion(13);

        public object Clone()
        {
            var clone = (DBADashContext)MemberwiseClone();
            clone._canMessage = null;
            return clone;
        }
    }
}