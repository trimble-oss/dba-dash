using DBADash;
using System;
using System.Diagnostics;
using System.Reflection;
using Topshelf;

namespace DBADashService
{
    class Program
    {
        public static readonly NamedLocker Locker = new NamedLocker();

        static void Main(string[] args)
        {
            Console.WriteLine(Properties.Resources.LogoText);
            var cfg = ScheduleService.GetConfig();
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
                Console.WriteLine("Service Name:" + cfg.ServiceName);
                x.SetDisplayName(cfg.ServiceName);
                x.SetServiceName(cfg.ServiceName);
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;
        }


    }



}
