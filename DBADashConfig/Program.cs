using CommandLine;
using DBADash;
using DBADashConfig;
using Serilog;
using static DBADashConfig.Options;

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
                  Console.WriteLine(config.SourceConnections.Count());
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
    Log.Error(ex,"Error running DBADashConfig");
    Environment.Exit(1);
}





