using CommandLine;
using DBADash;
using DBADashConfig;
using Serilog;
using System.Runtime.InteropServices;
using static DBADashConfig.Options;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
           .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {NewLine}{Exception}")
           .CreateLogger();

        return await Parser.Default
            .ParseArguments<Options>(args)
            .MapResult(
                async o =>
                {
                    try
                    {
                        await ProcessParsed(o);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unhandled error");
                        return 1;
                    }
                    finally
                    {
                        Log.CloseAndFlush();
                    }
                },
                _ => Task.FromResult(1));
    }

    private static async Task ProcessParsed(Options o)
    {
        CollectionConfig config;
        if (!BasicConfig.ConfigExists)
        {
            Log.Information("Config file does not exist. Starting with blank config");
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

        Log.Information("Action: {Action}", o.Option);

        switch (o.Option)
        {
            case CommandLineActionOption.GetServiceName:
                Console.WriteLine(config.ServiceName);
                break;

            case CommandLineActionOption.GetDestination:
                Console.WriteLine(config.DestinationConnection.EncryptedConnectionString);
                break;

            case CommandLineActionOption.List:
                Helper.ListConnections(config);
                break;

            case CommandLineActionOption.List2:
                Helper.ListConnections2(config);
                break;

            case CommandLineActionOption.Count:
                Console.WriteLine(config.SourceConnections.Count);
                break;

            case CommandLineActionOption.Add:
                await Helper.AddSourceConnectionAsync(config, o);
                break;

            case CommandLineActionOption.Remove:
                await Helper.RemoveSourceConnectionAsync(o, config, false);
                break;

            case CommandLineActionOption.RemoveAndDelete:
                await Helper.RemoveSourceConnectionAsync(o, config, true);
                break;

            case CommandLineActionOption.SetDestination:
            case CommandLineActionOption.AddDestination:
                await Helper.AddDestination(config, o);
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
                await Helper.PopulateConnectionIDAsync(config, o, false);
                break;

            case CommandLineActionOption.PopulateConnectionID2:
                await Helper.PopulateConnectionIDAsync(config, o, true);
                break;

            case CommandLineActionOption.SetAWS:
                Helper.SetAWS(config, o);
                break;
        }
    }
}