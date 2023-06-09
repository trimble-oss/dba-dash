using CommandLine;
using DBADash;
using DBADashConfig;
using Serilog;
using System.Runtime.InteropServices;
using Amazon.Auth.AccessControlPolicy;
using Microsoft.SqlServer.Dac.KeyVault;
using static DBADashConfig.Options;

static void CommandLineUpgrade()
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

        return;
    }
    catch (AggregateException ex) when (ex.InnerException != null &&
                                        ex.InnerException.GetType() == typeof(Octokit.NotFoundException))
    {
        Log.Error("Upgrade script is not available.  Please check the upgrade instructions on the GitHub page");
        return;
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error running upgrade");
        throw;
    }
}

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {NewLine}{Exception}")
    .CreateLogger();

CollectionConfig config;

try
{
    Parser.Default.ParseArguments<Options>(args)
          .WithParsed<Options>(o =>
          {
              // Read config from json file or create a new config if the file doesn't exist
              if (!(BasicConfig.ConfigExists))
              {
                  Log.Information("Config file does not exist.  Starting with blank config");
                  config = new CollectionConfig();
              }
              else
              {
                  config = BasicConfig.Load<CollectionConfig>(o.DecryptionPassword);
                  if (o.SavePassword && !string.IsNullOrEmpty(o.DecryptionPassword) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                  {
                      Log.Information("Saving password");
                      EncryptedConfig.SavePassword(o.DecryptionPassword);
                  }
              }

              Log.Information("Action:" + o.Option.ToString());
              switch (o.Option)
              {
                  // Just return the name of the service
                  case CommandLineActionOption.GetServiceName:
                      Console.WriteLine(config.ServiceName);
                      Environment.Exit(0);
                      break;

                  case CommandLineActionOption.GetDestination:
                      Console.WriteLine(config.DestinationConnection.EncryptedConnectionString);
                      Environment.Exit(0);
                      break;
                  // List source and destination connections
                  case CommandLineActionOption.List:
                      {
                          foreach (var cn in config.SourceConnections)
                          {
                              Console.WriteLine(cn.SourceConnection.EncryptedConnectionString);
                          }
                          Environment.Exit(0);
                          break;
                      }
                  // Count connections
                  case CommandLineActionOption.Count:
                      Console.WriteLine(config.SourceConnections.Count);
                      Environment.Exit(0);
                      break;
                  // Add a new source connection
                  case CommandLineActionOption.Add when string.IsNullOrEmpty(o.ConnectionString):
                      throw new ArgumentException("ConnectionString required");
                  case CommandLineActionOption.Add:
                      {
                          Log.Information("Add new connection: {Connection}", o.ConnectionString);
                          // check if connection exists before adding a new connection
                          if (config.SourceExists(o.ConnectionString))
                          {
                              if (o.Replace)
                              {
                                  Log.Information("Replace existing connection");
                                  config.SourceConnections.Remove(config.GetSourceFromConnectionString(o.ConnectionString));
                              }
                              else
                              {
                                  Log.Information("Source connection already exists");
                                  Environment.Exit(0);
                              }
                          }
                          var source = new DBADashSource()
                          {
                              IOCollectionLevel = (DBADashSource.IOCollectionLevels)o.IOCollectionLevel,
                              ConnectionString = o.ConnectionString,
                              NoWMI = o.NoWMI,
                              CollectSessionWaits = !o.NoCollectSessionWaits,
                              PlanCollectionEnabled = o.PlanCollectionEnabled,
                              SlowQueryThresholdMs = o.SlowQueryThresholdMs,
                              SlowQuerySessionMaxMemoryKB = o.SlowQuerySessionMaxMemoryKB,
                              SlowQueryTargetMaxMemoryKB = o.SlowQueryTargetMaxMemoryKB
                          };
                          if (o.ConnectionID != string.Empty)
                          {
                              source.ConnectionID = o.ConnectionID;
                          }
                          if (!string.IsNullOrEmpty(o.SchemaSnapshotDBs) && o.SchemaSnapshotDBs != "<null>") // <null> was added for powershell script as passing a blank string results in an error with commandline parser
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
                          if (!o.SkipValidation)
                          {
                              Log.Information("Validating connection...");
                              source.SourceConnection.Validate();
                              Log.Information("Validated");
                          }
                          config.SourceConnections.Add(source);
                          break;
                      }
                  // Remove connection from config
                  case CommandLineActionOption.Remove when string.IsNullOrEmpty(o.ConnectionString):
                      throw new ArgumentException("ConnectionString required");
                  case CommandLineActionOption.Remove when config.SourceExists(o.ConnectionString):
                      {
                          var remove = config.GetSourceFromConnectionString(o.ConnectionString);
                          Log.Information("Remove existing connection: {Connection}", remove.SourceConnection.ConnectionForPrint);
                          config.SourceConnections.Remove(remove);
                          break;
                      }
                  case CommandLineActionOption.Remove:
                      Log.Warning("Connection not found");
                      Environment.Exit(0);
                      break;
                  // Set/Update the destination connection
                  case CommandLineActionOption.SetDestination when string.IsNullOrEmpty(o.ConnectionString):
                      throw new ArgumentException("ConnectionString required");
                  case CommandLineActionOption.SetDestination:
                      {
                          Log.Information("Setting destination connection");
                          config.Destination = o.ConnectionString;
                          if (!o.SkipValidation)
                          {
                              Log.Information("Validating connection...");
                              config.ValidateDestination();
                              Log.Information("Validated");
                          }

                          break;
                      }
                  case CommandLineActionOption.CheckForUpdates:
                      {
                          var latest = Upgrade.GetLatestVersionAsync().GetAwaiter().GetResult();
                          Log.Information("Upgrade Available: : {0}", Upgrade.IsUpgradeAvailable(latest));
                          Log.Information("Current Version: {0}", Upgrade.CurrentVersion().ToString());
                          Log.Information("Latest Version: {0}", latest.TagName);
                          Log.Information("Release Date: {0}", latest.PublishedAt.ToString());
                          Log.Information("URL: {0}", latest.Url);
                          Console.WriteLine(latest.Body);
                          return;
                      }
                  case CommandLineActionOption.Update:
                      CommandLineUpgrade();
                      return;

                  case CommandLineActionOption.SetServiceName when string.IsNullOrEmpty(o.ServiceName):
                      Log.Error("ServiceName is required with SetServiceName action");
                      return;

                  case CommandLineActionOption.SetServiceName when o.ServiceName == config.ServiceName:
                      Log.Information("ServiceName is already set to {ServiceName}", config.ServiceName);
                      return;

                  case CommandLineActionOption.SetServiceName when RuntimeInformation.IsOSPlatform(OSPlatform.Windows):
                      {
                          if (ServiceTools.IsServiceInstalledByName(o.ServiceName)) // Check if a service exists with the specified name
                          {
                              Log.Error("ServiceName is already in use");
                              return;
                          }
                          if (DBADash.ServiceTools.IsServiceInstalledByPath()) // Check if the service is already installed by location on disk
                          {
                              Log.Error("Service is already installed.  Please uninstall before setting a new service name");
                              return;
                          }
                          else
                          {
                              Log.Information("Setting service name to {ServiceName}", o.ServiceName);
                              config.ServiceName = o.ServiceName;
                          }

                          break;
                      }
                  case CommandLineActionOption.Encrypt:
                      if (string.IsNullOrEmpty(o.EncryptionPassword))
                      {
                          Log.Error("EncryptionPassword parameter not supplied");
                          return;
                      }
                      EncryptedConfig.SetPassword(o.EncryptionPassword, true);
                      config.EncryptionOption = BasicConfig.EncryptionOptions.Encrypt;
                      break;

                  case CommandLineActionOption.Decrypt:
                      config.EncryptionOption = BasicConfig.EncryptionOptions.Basic;
                      break;

                  case CommandLineActionOption.SetServiceName:
                      Log.Error("SetServiceName is only supported on Windows");
                      return;
              }
              Log.Information("Saving config");
              config.Save(!o.NoBackupConfig);

              Log.Information("Complete.  Restart the service to apply the config change");
          });
}
catch (Exception ex)
{
    Log.Error(ex, "Error running DBADashConfig");
    Environment.Exit(1);
}