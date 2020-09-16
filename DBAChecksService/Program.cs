using DBAChecks;
using System;
using System.Diagnostics;
using System.Reflection;
using Topshelf;

namespace DBAChecksService
{
    class Program
    {
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
          

                x.SetDescription("Collect data from SQL Instances");
                x.SetDisplayName("DBAChecksService");
                x.SetServiceName("DBAChecksService");
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;
        }


    }



}
