using CommandLine;
using DBADash;
using DBADashConfig;
using Serilog;
using System.Runtime.InteropServices;
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

string jsonConfigPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ServiceConfig.json");
string backupPath = jsonConfigPath + ".backup_" + DateTime.Now.ToString("yyyyMMddHHmmssFFF");

// Read config from json file or create a new config if the file doesn't exist
if (!(System.IO.File.Exists(jsonConfigPath)))
{
    Log.Information("Config file does not exist.  Starting with blank config");
    config = new CollectionConfig();
}
else
{
    string jsonConfig = System.IO.File.ReadAllText(jsonConfigPath);
    config = CollectionConfig.Deserialize(jsonConfig);
}

try
{
    Parser.Default.ParseArguments<Options>(args)
          .WithParsed<Options>(o =>
          {
              Log.Information("Action:" + o.Option.ToString());
              if (o.Option == CommandLineActionOption.GetServiceName) // Just return the name of the service
              {
                  Console.WriteLine(config.ServiceName);
                  Environment.Exit(0);
              }
              else if (o.Option == CommandLineActionOption.GetDestination)
              {
                  Console.WriteLine(config.DestinationConnection.EncryptedConnectionString);
                  Environment.Exit(0);
              }
              else if (o.Option == CommandLineActionOption.List) // List source and destination connections
              {
                  foreach (var cn in config.SourceConnections)
                  {
                      Console.WriteLine(cn.SourceConnection.EncryptedConnectionString);
                  }
                  Environment.Exit(0);
              }
              else if (o.Option == CommandLineActionOption.Count) // Count connections
              {
                  Console.WriteLine(config.SourceConnections.Count);
                  Environment.Exit(0);
              }
              else if (o.Option == CommandLineActionOption.Add) // Add a new source connection
              {
                  if (string.IsNullOrEmpty(o.ConnectionString))
                  {
                      throw new ArgumentException("ConnectionString required");
                  }
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
                  var source = new DBADashSource();
                  if (o.ConnectionID != string.Empty)
                  {
                      source.ConnectionID = o.ConnectionID;
                  }
                  source.ConnectionString = o.ConnectionString;
                  source.NoWMI = o.NoWMI;
                  source.CollectSessionWaits = !o.NoCollectSessionWaits;
                  if (!string.IsNullOrEmpty(o.SchemaSnapshotDBs) && o.SchemaSnapshotDBs != "<null>") // <null> was added for powershell script as passing a blank string results in an error with commandline parser
                  {
                      source.SchemaSnapshotDBs = o.SchemaSnapshotDBs;
                  }
                  source.PlanCollectionEnabled = o.PlanCollectionEnabled;
                  if (o.PlanCollectionEnabled)
                  {
                      source.PlanCollectionCountThreshold = o.PlanCollectionCountThreshold;
                      source.PlanCollectionCPUThreshold = o.PlanCollectionCPUThreshold;
                      source.PlanCollectionDurationThreshold = o.PlanCollectionDurationThreshold;
                      source.PlanCollectionMemoryGrantThreshold = o.PlanCollectionMemoryGrantThreshold;
                  }
                  source.SlowQueryThresholdMs = o.SlowQueryThresholdMs;
                  source.SlowQuerySessionMaxMemoryKB = o.SlowQuerySessionMaxMemoryKB;
                  source.SlowQueryTargetMaxMemoryKB = o.SlowQueryTargetMaxMemoryKB;
                  if (!o.SkipValidation)
                  {
                      Log.Information("Validating connection...");
                      source.SourceConnection.Validate();
                      Log.Information("Validated");
                  }
                  config.SourceConnections.Add(source);
              }
              else if (o.Option == CommandLineActionOption.Remove) // Remove connection from config
              {
                  if (string.IsNullOrEmpty(o.ConnectionString))
                  {
                      throw new ArgumentException("ConnectionString required");
                  }
                  if (config.SourceExists(o.ConnectionString))
                  {
                      var remove = config.GetSourceFromConnectionString(o.ConnectionString);
                      Log.Information("Remove existing connection: {Connection}", remove.SourceConnection.ConnectionForPrint);
                      config.SourceConnections.Remove(remove);
                  }
                  else
                  {
                      Log.Warning("Connection not found");
                      Environment.Exit(0);
                  }
              }
              else if (o.Option == CommandLineActionOption.SetDestination) // Set/Update the destination connection
              {
                  if (string.IsNullOrEmpty(o.ConnectionString))
                  {
                      throw new ArgumentException("ConnectionString required");
                  }
                  Log.Information("Setting destination connection");
                  config.Destination = o.ConnectionString;
                  if (!o.SkipValidation)
                  {
                      Log.Information("Validating connection...");
                      config.ValidateDestination();
                      Log.Information("Validated");
                  }
              }
              else if (o.Option == CommandLineActionOption.CheckForUpdates)
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
              else if (o.Option == CommandLineActionOption.Update)
              {
                  CommandLineUpgrade();
                  return;
              }
              else if (o.Option == CommandLineActionOption.SetServiceName)
              {
                  if (string.IsNullOrEmpty(o.ServiceName))
                  {
                      Log.Error("ServiceName is required with SetServiceName action");
                      return;
                  }
                  if (o.ServiceName == config.ServiceName)
                  {
                      Log.Information("ServiceName is already set to {ServiceName}", config.ServiceName);
                      return;
                  }
                  if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                  }
                  else
                  {
                      Log.Error("SetServiceName is only supported on Windows");
                      return;
                  }
              }
              // Save a copy of the old config before writing changes
              if (File.Exists(jsonConfigPath) && !o.NoBackupConfig)
              {
                  Log.Information("Saving old config to: {path}", backupPath);
                  File.Move(jsonConfigPath, backupPath);
              }

              File.WriteAllText(jsonConfigPath, config.Serialize());

              Log.Information("Complete.  Restart the service to apply the config change");
          });
}
catch (Exception ex)
{
    Log.Error(ex, "Error running DBADashConfig");
    Environment.Exit(1);
}