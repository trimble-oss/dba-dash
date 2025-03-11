using CommandLine;
using DBADash;
using DBADashConfig;
using Serilog;
using System.Runtime.InteropServices;
using static DBADashConfig.Options;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {NewLine}{Exception}")
    .CreateLogger();

CollectionConfig config;

try
{
    Parser.Default.ParseArguments<Options>(args)
          .WithParsed(o =>
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

              Log.Information("Action:" + o.Option);
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
                      Helper.ListConnections(config);
                      break;

                  case CommandLineActionOption.List2:
                      Helper.ListConnections2(config);
                      break;
                  // Count connections
                  case CommandLineActionOption.Count:
                      Console.WriteLine(config.SourceConnections.Count);
                      Environment.Exit(0);
                      break;
                  // Add a new source connection
                  case CommandLineActionOption.Add:
                      Helper.AddSourceConnection(config, o);
                      break;

                  case CommandLineActionOption.Remove:
                      Helper.RemoveSourceConnection(o, config, false);
                      break;

                  case CommandLineActionOption.RemoveAndDelete:
                      Helper.RemoveSourceConnection(o, config, true);
                      break;

                  case CommandLineActionOption.SetDestination:
                  case CommandLineActionOption.AddDestination:
                      Helper.AddDestination(config, o);
                      break;

                  case CommandLineActionOption.RemoveDestination:
                      Helper.RemoveDestination(config, o);
                      break;

                  case CommandLineActionOption.CheckForUpdates:
                      Helper.CheckForUpdates();
                      break;

                  case CommandLineActionOption.Update:
                      Helper.CommandLineUpgrade();
                      break;

                  case CommandLineActionOption.SetServiceName:
                      Helper.SetServiceName(config, o);
                      break;

                  case CommandLineActionOption.Encrypt:
                      Helper.EncryptConfig(config, o);
                      break;

                  case CommandLineActionOption.Decrypt:
                      config.EncryptionOption = BasicConfig.EncryptionOptions.Basic;
                      Helper.SaveConfig(config, o);
                      break;

                  case CommandLineActionOption.SetConfigFileBackupRetention:
                      config.ConfigBackupRetentionDays = o.RetentionDays;
                      Helper.SaveConfig(config, o);
                      break;

                  case CommandLineActionOption.Delete:
                      Helper.MarkInstanceDeleted(config, o.ConnectionID);
                      break;

                  case CommandLineActionOption.Restore:
                      Helper.MarkInstanceDeleted(config, o.ConnectionID, true);
                      break;

                  case CommandLineActionOption.PopulateConnectionID:
                      Helper.PopulateConnectionID(config, o,false);
                      break;
                  case CommandLineActionOption.PopulateConnectionID2:
                      Helper.PopulateConnectionID(config, o,true);
                      break;
              }
          });
}
catch (Exception ex)
{
    Log.Error(ex, ex.Message);
    Environment.Exit(1);
}