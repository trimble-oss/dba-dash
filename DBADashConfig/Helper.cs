using DBADash;
using Serilog;
using System.Runtime.InteropServices;

namespace DBADashConfig
{
    public class Helper
    {
        public static void CommandLineUpgrade()
        {
            try
            {
                var latest = Upgrade.GetLatestVersionAsync().GetAwaiter().GetResult();
                if (Upgrade.IsUpgradeIncomplete)
                {
                    Log.Warning(Upgrade.IncompleteUpgradeMessage);
                    Log.Information("Upgrade will be attempted");
                }
                if (Upgrade.IsUpgradeAvailable(latest) || Upgrade.IsUpgradeIncomplete)
                {
                    if (Upgrade.IsAdministrator)
                    {
                        Log.Information($"Upgrade is available to {latest.TagName}.  Initiating upgrade.");
                        Upgrade.UpgradeDBADashAsync(noExit: false).Wait();
                    }
                    else
                    {
                        Log.Information($"Upgrade is available to {latest.TagName}.  Please re-run as Administrator.");
                    }
                }
                else
                {
                    Log.Information($"Latest version is {latest.TagName}.  Upgrade is not available at this time. ");
                }
            }
            catch (AggregateException ex) when (ex.InnerException != null &&
                                                ex.InnerException.GetType() == typeof(Octokit.NotFoundException))
            {
                Log.Error("Upgrade script is not available.  Please check the upgrade instructions on the GitHub page");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running upgrade");
                Environment.Exit(1);
            }
        }

        public static async Task<DBADashSource?> GetSourceConnectionAsync(Options o, CollectionConfig config)
        {
            if (!string.IsNullOrEmpty(o.ConnectionString))
            {
                return config.GetSourceFromConnectionString(o.ConnectionString);
            }
            else if (!string.IsNullOrEmpty(o.ConnectionID))
            {
                try
                {
                    return await config.GetSourceConnectionAsync(o.ConnectionID);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "ConnectionID not found: {ConnectionID}", o.ConnectionID);
                    Environment.Exit(1);
                }
            }
            else
            {
                Log.Error("ConnectionString or ConnectionID required");
                Environment.Exit(1);
            }

            return null;
        }

        public static async Task RemoveSourceConnectionAsync(Options o, CollectionConfig config, bool delete)
        {
            var sourceToRemove = await GetSourceConnectionAsync(o, config);
            if (sourceToRemove == null)
            {
                Log.Error("Source connection not found.");
                Environment.Exit(1);
                return;
            }

            Log.Information("Remove existing connection: {Connection}", sourceToRemove.SourceConnection.ConnectionForPrint);
            config.SourceConnections.Remove(sourceToRemove);
            if (delete)
            {
                MarkInstanceDeleted(config, sourceToRemove.ConnectionID);
            }
            SaveConfig(config, o);
        }

        public static void MarkInstanceDeleted(CollectionConfig config, string connectionId, bool isActive = false)
        {
            var status = isActive ? "active" : "deleted";
            if (string.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentException("ConnectionID required");
            }
            foreach (var dest in config.AllDestinations.Where(d => d.Type == DBADashConnection.ConnectionType.SQL))
            {
                Log.Information("Marking instance {status} in {Destination}", status, dest.ConnectionForPrint);
                try
                {
                    if (isActive)
                    {
                        SharedData.RestoreInstance(connectionId, dest.ConnectionString);
                    }
                    else
                    {
                        SharedData.MarkInstanceDeleted(connectionId, dest.ConnectionString);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error marking instance {status} in {Destination}", status, dest.ConnectionForPrint);
                    Environment.Exit(1);
                }
            }
        }

        public static void CheckForUpdates()
        {
            var latest = Upgrade.GetLatestVersionAsync().GetAwaiter().GetResult();
            Log.Information("Upgrade Available: : {0}", Upgrade.IsUpgradeAvailable(latest));
            Log.Information("Current Version: {0}", Upgrade.CurrentVersion().ToString());
            Log.Information("Latest Version: {0}", latest.TagName);
            Log.Information("Release Date: {0}", latest.PublishedAt.ToString());
            Log.Information("URL: {0}", latest.Url);
            Console.WriteLine(latest.Body);
        }

        public static async Task AddSourceConnectionAsync(CollectionConfig config, Options o)
        {
            if (string.IsNullOrEmpty(o.ConnectionString))
            {
                Log.Error("ConnectionString required");
                Environment.Exit(1);
                return;
            }

            Log.Information("Add new connection: {Connection}", o.ConnectionString);

            var source = new DBADashSource()
            {
                IOCollectionLevel = (DBADashSource.IOCollectionLevels)o.IOCollectionLevel,
                ConnectionString = o.ConnectionString,
                NoWMI = o.NoWMI,
                CollectSessionWaits = !o.NoCollectSessionWaits,
                PlanCollectionEnabled = o.PlanCollectionEnabled,
                SlowQueryThresholdMs = o.SlowQueryThresholdMs,
                SlowQuerySessionMaxMemoryKB = o.SlowQuerySessionMaxMemoryKB,
                SlowQueryTargetMaxMemoryKB = o.SlowQueryTargetMaxMemoryKB,
                UseDualEventSession = o.UseDualEventSession ?? true,
                PersistXESessions = o.PersistXESessions ?? false,
                CollectTempDB = o.CollectTempDB ?? false,
                CollectTranBeginTime = o.CollectTranBeginTime ?? true,
                WriteToSecondaryDestinations = o.WriteToSecondaryDestinations ?? true,
                ScriptAgentJobs = o.ScriptAgentJobs ?? true,
            };
            if (!o.SkipValidation)
            {
                Log.Information("Validating connection...");
                source.SourceConnection.Validate();
                Log.Information("Validated");
            }
            if (o.ConnectionID != string.Empty)
            {
                source.ConnectionID = o.ConnectionID;
            }
            else if (!o.SkipValidation)
            {
                source.ConnectionID = await source.GetGeneratedConnectionIDAsync();
            }
            else if (source.SourceConnection.Type == DBADashConnection.ConnectionType.SQL)
            {
                source.SetConnectionIDFromBuilderIfNotSet();
                Log.Warning("Validation skipped & ConnectionID not specified. ConnectionID set to {ConnectionID} based on Data Source", source.ConnectionID);
            }
            if (!string.IsNullOrEmpty(o.SchemaSnapshotDBs) && o.SchemaSnapshotDBs != "<null>") // <null> was added for PowerShell script as passing a blank string results in an error with commandline parser
            {
                source.SchemaSnapshotDBs = o.SchemaSnapshotDBs;
            }
            if (o.PlanCollectionEnabled)
            {
                source.PlanCollectionCountThreshold = o.PlanCollectionCountThreshold;
                source.PlanCollectionCPUThreshold = o.PlanCollectionCPUThreshold;
                source.PlanCollectionDurationThreshold = o.PlanCollectionDurationThreshold;
                source.PlanCollectionMemoryGrantThreshold = o.PlanCollectionMemoryGrantThreshold;
            }
            // check if connection exists before adding a new connection
            var oldSource = await config.FindSourceConnectionAsync(o.ConnectionString, source.ConnectionID);
            if (oldSource != null)
            {
                if (o.Replace)
                {
                    Log.Information("Replace existing connection");
                    config.SourceConnections.Remove(oldSource);
                }
                else
                {
                    Log.Warning("Source connection already exists.  Use --Replace to update the existing connection.");
                    Environment.Exit(0);
                }
            }
            config.SourceConnections.Add(source);
            SaveConfig(config, o);
        }

        public static void ListConnections(CollectionConfig config)
        {
            foreach (var cn in config.SourceConnections)
            {
                Console.WriteLine(cn.SourceConnection.EncryptedConnectionString);
            }
        }

        public static void ListConnections2(CollectionConfig config)
        {
            foreach (var cn in config.SourceConnections)
            {
                Console.WriteLine(cn.ConnectionID + "\t" + cn.SourceConnection.EncryptedConnectionString);
            }
        }

        public static void AddDestination(CollectionConfig config, Options o)
        {
            if (string.IsNullOrEmpty(o.ConnectionString))
            {
                Log.Error("ConnectionString required");
                Environment.Exit(1);
                return;
            }
            if (string.IsNullOrEmpty(config.Destination) || o.Option == Options.CommandLineActionOption.SetDestination) // Set primary destination
            {
                Log.Information("Setting destination connection");
                config.Destination = o.ConnectionString;
                if (!o.SkipValidation)
                {
                    Log.Information("Validating connection...");
                    config.ValidateDestination();
                    Log.Information("Validated");
                }
            }
            else // Add additional destination
            {
                if (config.AllDestinations.Any(d => d.ConnectionString == o.ConnectionString))
                {
                    Log.Information("Destination connection already exists");
                    return;
                }

                var con = new DBADashConnection(o.ConnectionString);
                if (!o.SkipValidation)
                {
                    Log.Information("Validating connection...");

                    CollectionConfig.ValidateDestination(con);
                    Log.Information("Validated");
                }

                Log.Information("Adding secondary destination");
                config.SecondaryDestinationConnections.Add(con);
            }
            SaveConfig(config, o);
        }

        public static void RemoveDestination(CollectionConfig config, Options o)
        {
            if (string.IsNullOrEmpty(o.ConnectionString))
            {
                Log.Error("ConnectionString required");
                Environment.Exit(1);
                return;
            }
            var toRemove = new DBADashConnection(o.ConnectionString);
            if (config.Destination == o.ConnectionString || config.DestinationConnection.ConnectionForPrint == toRemove.ConnectionForPrint)
            {
                config.Destination = string.Empty;
                Log.Warning("Warning: Primary Destination removed.");
            }
            else
            {
                var found = config.SecondaryDestinationConnections.FirstOrDefault(d =>
                    d.ConnectionString == o.ConnectionString ||
                    d.ConnectionForPrint == toRemove.ConnectionForPrint);
                if (found == null)
                {
                    Log.Error("Destination connection not found");
                    Environment.Exit(1);
                    return;
                }

                config.SecondaryDestinationConnections.Remove(found);
            }
            SaveConfig(config, o);
        }

        public static void SetServiceName(CollectionConfig config, Options o)
        {
            if (string.IsNullOrEmpty(o.ServiceName))
            {
                Log.Error("ServiceName is required with SetServiceName action");
                Environment.Exit(1);
            }
            else if (o.ServiceName == config.ServiceName)
            {
                Log.Information("ServiceName is already set to {ServiceName}", config.ServiceName);
            }
            else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.Error("SetServiceName is only supported on Windows");
                Environment.Exit(1);
            }
            else if (ServiceTools.IsServiceInstalledByName(o.ServiceName)) // Check if a service exists with the specified name
            {
                Log.Error("ServiceName is already in use");
                Environment.Exit(1);
            }
            else if (ServiceTools.IsServiceInstalledByPath()) // Check if the service is already installed by location on disk
            {
                Log.Error("Service is already installed.  Please uninstall before setting a new service name");
                Environment.Exit(1);
            }
            else
            {
                Log.Information("Setting service name to {ServiceName}", o.ServiceName);
                config.ServiceName = o.ServiceName;
                SaveConfig(config, o);
            }
        }

        public static void SaveConfig(CollectionConfig config, Options o)
        {
            Log.Information("Saving config");

            var backup = config.ConfigBackupRetentionDays > 0 && (!o.NoBackupConfig);
            config.Save(backup);

            Log.Information("Complete.  Restart the service to apply the config change");
        }

        public static void EncryptConfig(CollectionConfig config, Options o)
        {
            if (string.IsNullOrEmpty(o.EncryptionPassword))
            {
                Log.Error("EncryptionPassword parameter not supplied");
                Environment.Exit(1);
                return;
            }
            EncryptedConfig.SetPassword(o.EncryptionPassword, true);
            config.EncryptionOption = BasicConfig.EncryptionOptions.Encrypt;
            SaveConfig(config, o);
        }

        public static async Task PopulateConnectionIDAsync(CollectionConfig config, Options o, bool force)
        {
            var errors = 0;
            var succeeded = 0;
            foreach (var source in config.SourceConnections.Where(src => src.SourceConnection.Type == DBADashConnection.ConnectionType.SQL && string.IsNullOrEmpty(src.ConnectionID)))
            {
                try
                {
                    source.ConnectionID = await source.GetGeneratedConnectionIDAsync();
                    Log.Information("ConnectionID {ConnectionID} generated", source.ConnectionID);
                    succeeded++;
                }
                catch (Exception ex)
                {
                    if (force)
                    {
                        source.SetConnectionIDFromBuilderIfNotSet();
                        succeeded++;
                        Log.Warning(ex, "Error generating ConnectionID for {ConnectionString}.  Connection set to {ConnectionID} from connection string builder.", source.SourceConnection.ConnectionForPrint, source.ConnectionID);
                    }
                    else
                    {
                        Log.Error(ex, "Error generating ConnectionID for {ConnectionString}", source.SourceConnection.ConnectionForPrint);
                        errors++;
                    }
                }
            }
            if (succeeded > 0)
            {
                Log.Information("ConnectionID generated for {Count} source connections.", succeeded);
                SaveConfig(config, o);
            }
            if (errors == 0 && succeeded == 0)
            {
                Log.Information("No source connections found without a ConnectionID");
            }
            if (errors > 0)
            {
                Log.Error("{Count} errors occurred generating ConnectionID.  Please review the log for details.", errors);
                Environment.Exit(1);
            }
        }
    }
}