using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Dac;
using Serilog;
using static DBADash.DBValidations;

namespace DacpacUtility
{
    public class DacpacService
    {
        public List<string> MessageList { get; set; }

        public DacpacService()
        {
            MessageList = new List<string>();
            _dacDeployOptions = new DacDeployOptions
            {
                IgnorePermissions = true,
                IgnoreTableOptions = true,
                IgnorePartitionSchemes = true,
                IgnoreIndexOptions = true,
                IgnoreUserSettingsObjects = true,
                IgnoreRoleMembership = true,
                DropObjectsNotInSource = true,
                BlockOnPossibleDataLoss = true,
                RegisterDataTierApplication = false,
                BlockWhenDriftDetected = false,
                ExcludeObjectTypes = new[]
                {
                    ObjectType.Users,
                    ObjectType.Logins,
                    ObjectType.RoleMembership,
                    ObjectType.Permissions
                }
            };
        }

        public void ProcessDacPac(string connectionString,
                                    string databaseName,
                                    string dacpacName,
                                    DBVersionStatusEnum status)
        {

            MessageList.Add("*** Start of processing for " +
                             databaseName);

            _dacDeployOptions.ScriptDatabaseOptions = status == DBVersionStatusEnum.CreateDB;
            if (!_dacDeployOptions.ScriptDatabaseOptions)
            {
                MessageList.Add("Skipping deployment of Database Options for existing database");
            }

            var dacServiceInstance = new DacServices(connectionString);
            dacServiceInstance.ProgressChanged +=
              new EventHandler<DacProgressEventArgs>((s, e) => {
                  MessageList.Add(e.Message);
                  Log.Information(e.Message);
              });
            dacServiceInstance.Message +=
              new EventHandler<DacMessageEventArgs>((s, e) =>
              {
                  MessageList.Add(e.Message.Message);
                  Log.Information(e.Message.Message);
              });

            try
            {
                using (DacPackage dacpac = DacPackage.Load(dacpacName))
                {
                    if (_dacDeployOptions.SqlCommandVariableValues.ContainsKey("VersionNumber"))
                    {
                        _dacDeployOptions.SqlCommandVariableValues.Remove("VersionNumber");
                    }
                    _dacDeployOptions.SqlCommandVariableValues.Add("VersionNumber", dacpac.Version.ToString());
                    dacServiceInstance.Deploy(dacpac, databaseName,
                                            upgradeExisting: true,
                                            options: _dacDeployOptions);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing dacpac");
                MessageList.Add(ex.Message);
                throw;
            }
        }

        DacDeployOptions _dacDeployOptions;

        public DacDeployOptions DacDeployOpts
        {
            get
            {
                return _dacDeployOptions;
            }
            set
            {
                _dacDeployOptions = value;
            }
        }

        public System.Version GetVersion(string dacpacName)
        {
            using (DacPackage dacpac = DacPackage.Load(dacpacName))
            {
                return dacpac.Version;
            }
        }


        public string GenerateDeployScript(string connectionString,
                                string databaseName,
                                string dacpacName)
        {
  
            MessageList.Add("*** Start of processing for " +
                             databaseName);


            var dacServiceInstance = new DacServices(connectionString);
            dacServiceInstance.ProgressChanged +=
              new EventHandler<DacProgressEventArgs>((s, e) =>
                            MessageList.Add(e.Message));
            dacServiceInstance.Message +=
              new EventHandler<DacMessageEventArgs>((s, e) =>
                            MessageList.Add(e.Message.Message));

            try
            {
                using (DacPackage dacpac = DacPackage.Load(dacpacName))
                {
                    if (_dacDeployOptions.SqlCommandVariableValues.ContainsKey("VersionNumber"))
                    {
                        _dacDeployOptions.SqlCommandVariableValues.Remove("VersionNumber");
                    }
                    _dacDeployOptions.SqlCommandVariableValues.Add("VersionNumber", dacpac.Version.ToString());
                    return dacServiceInstance.GenerateDeployScript(dacpac, databaseName,
                                            options: _dacDeployOptions);
                }

            }
            catch (Exception ex)
            {
                MessageList.Add(ex.Message);
                return null;
            }

        }

    }
}