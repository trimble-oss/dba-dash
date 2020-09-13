using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Dac;

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
                RegisterDataTierApplication = true,
                ExcludeObjectTypes = new[]
                {
                    ObjectType.Users,
                    ObjectType.Logins,
                    ObjectType.RoleMembership,
                    ObjectType.Permissions
                }

            };
       
        }

        public bool ProcessDacPac(string connectionString,
                                    string databaseName,
                                    string dacpacName)
        {
            bool success = true;

            MessageList.Add("*** Start of processing for " +
                             databaseName);

            var dacOptions = new DacDeployOptions();
            dacOptions.BlockOnPossibleDataLoss = false;

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
                    dacServiceInstance.Deploy(dacpac, databaseName,
                                            upgradeExisting: true,
                                            options: dacOptions);
                }

            }
            catch (Exception ex)
            {
                success = false;
                MessageList.Add(ex.Message);
            }

            return success;
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


        public string GenerateDeployScript(string connectionString,
                                string databaseName,
                                string dacpacName)
        {
            bool success = true;

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
                    return dacServiceInstance.GenerateDeployScript(dacpac, databaseName,
                                            options: _dacDeployOptions);
                }

            }
            catch (Exception ex)
            {
                success = false;
                MessageList.Add(ex.Message);
                return null;
            }

        }

    }
}