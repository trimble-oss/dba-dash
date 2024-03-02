using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using CommandLine;
using DBADash;
using Microsoft.DotNet.PlatformAbstractions;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using CommandLine.Text;

namespace DBADashService
{
    internal class Program
    {
        public static readonly NamedLocker Locker = new();

        private static void Main(string[] args)
        {
            SetupLogging();
            Console.WriteLine(Properties.Resources.LogoText);
            Log.Information("Running as service {RunningAsService}", !Environment.UserInteractive);
            var cfg = SchedulerServiceConfig.Config;

            if (Environment.UserInteractive && ProcessCommandLine(args, cfg)) // Commandline options are not processed when running as a service
            {
                return;
            }

            if (DBADash.Upgrade.IsUpgradeIncomplete)
            {
                const string message =
                    $"Incomplete upgrade of DBA Dash detected.  File '{DBADash.Upgrade.UpgradeFile}' found in directory. Upgrade might have failed due to locked files. More info: https://dbadash.com/upgrades/";
                Log.Logger.Error(message);
                throw new Exception(message);
            }

            var builder = Host.CreateApplicationBuilder();

            // Configure the ShutdownTimeout to infinite
            builder.Services.Configure<HostOptions>(options =>
                options.ShutdownTimeout = Timeout.InfiniteTimeSpan);
            builder.Services.AddWindowsService(options => { options.ServiceName = cfg.ServiceName; });
            builder.Services.AddHostedService<ScheduleService>();

            var host = builder.Build();
            host.Run();
        }

        private static void SetupLogging()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory); //  for Logs folder
            // https://swimburger.net/blog/dotnet/changing-serilog-minimum-level-without-application-restart-on-dotnet-framework-and-core
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                // reloadOnChange will allow you to auto reload the minimum level and level switches
                .AddJsonFile(path: "serilog.json", optional: false, reloadOnChange: true)
                .Build();
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("ApplicationName", "DBADash")
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .CreateLogger();
        }

        [SupportedOSPlatformGuard("windows")]
        private static bool ProcessCommandLine(string[] args, CollectionConfig cfg)
        {
            var result = false;
            if (!OperatingSystem.IsWindows())
            {
                if (args.Length <= 0) return false;
                Log.Error("Command line options are not supported on this platform.");
                return true;
            }
            Parser.Default.ParseArguments<InstallOptions, StartOptions, StopOptions, UninstallOptions, RunOptions>(args)
                    .WithParsed<InstallOptions>(opts =>
                        {
                            if (!OperatingSystem.IsWindows()) return;
                            string userName = null;
                            if (!string.IsNullOrEmpty(opts.Username))
                            {
                                userName = opts.Username;
                            }
                            else if (opts.LocalService)
                            {
                                userName = "NT AUTHORITY\\LocalService";
                            }
                            else if (opts.NetworkService)
                            {
                                userName = "NT AUTHORITY\\NetworkService";
                            }
                            else if (opts.LocalSystem)
                            {
                                userName = "NT AUTHORITY\\SYSTEM";
                            }

                            var mode = opts.Delayed ? ServiceTools.StartMode.AutomaticDelayedStart :
                                opts.Disabled ? ServiceTools.StartMode.Disabled :
                                opts.Manual ? ServiceTools.StartMode.Manual :
                                opts.AutoStart ? ServiceTools.StartMode.Automatic :
                                ServiceTools.StartMode.AutomaticDelayedStart;

                            Console.WriteLine($"Installing service: {cfg.ServiceName}");
                            if (ServiceTools.IsServiceInstalledByName(cfg.ServiceName))
                            {
                                Log.Error($"Service {cfg.ServiceName} already exists.");
                            }
                            else
                            {
                                ServiceTools.InstallService(cfg.ServiceName, userName, opts.Password, mode);
                            }

                            result = true;
                        }
                    )
                    .WithParsed<UninstallOptions>(opts =>
                    {
                        if (!OperatingSystem.IsWindows()) return;
                        if (!ServiceTools.IsServiceInstalledByName(cfg.ServiceName))
                        {
                            Log.Error($"Service {cfg.ServiceName} has already been removed.");
                        }
                        else
                        {
                            Console.WriteLine($"Uninstalling service: {cfg.ServiceName}");
                            ServiceTools.UninstallService(cfg.ServiceName);
                        }

                        result = true;
                    })
                    .WithParsed<StopOptions>(opts =>
                    {
                        if (!OperatingSystem.IsWindows()) return;
                        if (!ServiceTools.IsServiceInstalledByName(cfg.ServiceName))
                        {
                            Log.Error($"Service {cfg.ServiceName} does not exist");
                        }
                        else
                        {
                            Console.WriteLine($"Stopping service: {cfg.ServiceName}");
                            var status = ServiceTools.StopService(cfg.ServiceName);
                            Console.WriteLine($"Service status: {status}");
                        }

                        result = true;
                    })
                    .WithParsed<StartOptions>(opts =>
                    {
                        if (!OperatingSystem.IsWindows()) return;
                        if (!ServiceTools.IsServiceInstalledByName(cfg.ServiceName))
                        {
                            Log.Error($"Service {cfg.ServiceName} does not exist");
                        }
                        else
                        {
                            Console.WriteLine($"Starting service: {cfg.ServiceName}");
                            var status = ServiceTools.StartService(cfg.ServiceName);
                            Console.WriteLine($"Service status: {status}");
                        }

                        result = true;
                    })
                    .WithNotParsed(errors =>
                    {
                        if (errors.Any(e => e is not HelpRequestedError && e is not HelpVerbRequestedError && e is not VersionRequestedError))
                        {
                            var sentenceBuilder = SentenceBuilder.Create();
                            foreach (var error in errors)
                            {
                                Log.Error(sentenceBuilder.FormatError(error));
                            }
                        }

                        result = true;
                    });

            return result;
        }
    }
}