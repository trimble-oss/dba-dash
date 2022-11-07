using DBADash;
using System;
using System.Diagnostics;
using System.Reflection;
using Topshelf;
using Serilog;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace DBADashService
{
    class Program
    {
        public static readonly NamedLocker Locker = new();

        static void Main(string[] args)
        {
            Console.WriteLine(Properties.Resources.LogoText);
            var cfg = SchedulerServiceConfig.Config;

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
               .Enrich.WithProperty("ServiceName", cfg.ServiceName)
               .Enrich.WithProperty("MachineName", Environment.MachineName)
               .CreateLogger();
  
            var rc = HostFactory.Run(x =>
            {
                x.Service<ScheduleService>(s =>
                {
                    s.ConstructUsing(name => new ScheduleService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.StartAutomaticallyDelayed();
                x.EnableServiceRecovery(r =>
                {
                    r.RestartService(1);
                });

                x.SetDescription("DBADash Service - SQL Server monitoring tool");
                Log.Logger.Information("Service Name {ServiceName}", cfg.ServiceName);
                x.SetDisplayName(cfg.ServiceName);
                x.SetServiceName(cfg.ServiceName);
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;
        }


    }



}
