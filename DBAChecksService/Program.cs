using DBAChecks;
using System;
using System.Diagnostics;
using System.Reflection;
using Topshelf;

namespace DBAChecksService
{
    class Program
    {
        public static readonly NamedLocker Locker = new NamedLocker();

        static void Main(string[] args)
        {
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

                x.SetDescription("DBAChecks Service - SQL Server monitoring tool");
                x.SetDisplayName(Properties.Settings.Default.ServiceName);
                x.SetServiceName(Properties.Settings.Default.ServiceName);
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;
        }


    }



}
