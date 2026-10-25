using DBADash;
using Serilog;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DBADashService
{
    internal class SchedulerServiceConfig
    {
        public static readonly CollectionConfig Config;
        public static readonly string FailedMessageFolder;

        static SchedulerServiceConfig()
        {
            FailedMessageFolder = Path.Combine(AppContext.BaseDirectory, "Failed");
            try
            {
                if (!Directory.Exists(FailedMessageFolder))
                {
                    Directory.CreateDirectory(FailedMessageFolder);
                }
            }
            catch
            {
                Log.Error("Error creating failed message folder {FailedMessageFolder}", FailedMessageFolder);
                FailedMessageFolder = string.Empty;
            }

            try
            {
                GetCreds();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetCreds error");
                throw;
            }

            try
            {
                Config = GetConfig();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetConfig error");
                throw;
            }
        }

        public static void GetCreds()
        {
            if (EncryptedConfig.HasTempKey && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !Environment.UserInteractive)
            {
                Log.Information("Saving config password");
                string password = EncryptedConfig.GetPassword();
                EncryptedConfig.SavePassword(password);
                Log.Information("Deleting temp key");
                EncryptedConfig.DeleteTempKey(true);
            }
        }

        public static CollectionConfig GetConfig()
        {
            if (!(BasicConfig.ConfigExists))
            {
                Log.Fatal("ServiceConfig.json file is missing.  Use service config tool to create.");
                throw new Exception("ServiceConfig.json file is missing.  Use service config tool to create.");
            }
            var conf = BasicConfig.Load<CollectionConfig>();
            foreach (var unreadable in conf.GetUnreadableSecrets())
            {
                Log.Warning("Unable to decrypt secret.  {Secret}", unreadable);
            }
            if (conf.WasEncrypted())
            {
                Log.Information("Saving ServiceConfig.json with encrypted password");
                conf.Save();
            }
            else if (conf.RequiresSecretUpgrade())
            {
                Log.Information("Saving ServiceConfig.json to upgrade secret protection");
                conf.Save();
            }

            // Don't attempt logins with a blank password - repeated failures can lock out the login
            if (conf.DestinationConnection.SecretError != null)
            {
                throw new Exception($"Unable to decrypt the password for the destination {conf.DestinationConnection.ConnectionForPrint}.  {conf.DestinationConnection.SecretError}");
            }
            foreach (var removed in conf.RemoveConnectionsWithUnreadableSecrets())
            {
                Log.Error("{Connection} won't be collected until its password is re-entered in the service config tool", removed);
            }

            return conf;
        }
    }
}