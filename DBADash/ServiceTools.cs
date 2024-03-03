using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Octokit;
using System;
using System.CodeDom;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.ServiceProcess;
using Serilog;
using System.Text;

namespace DBADash
{
    [SupportedOSPlatform("windows")]
    public static class ServiceTools
    {
        public static bool IsServiceInstalledByPath(string path)
        {
            using var searcher = new ManagementObjectSearcher("root\\cimv2", "SELECT Name,PathName from Win32_Service");

            var collection = searcher.Get().Cast<ManagementBaseObject>()
                .Where(service => ((string)service.GetPropertyValue("PathName"))?.Contains(path) == true)
                .Select(service => (string)service.GetPropertyValue("Name"));

            return collection.Any();
        }

        public static bool IsServiceInstalledByPath()
        {
            return IsServiceInstalledByPath(ServicePath);
        }

        public static bool IsServiceInstalledByName(string ServiceName)
        {
            using var searcher = new ManagementObjectSearcher("root\\cimv2", "SELECT Name from Win32_Service");

            var collection = searcher.Get().Cast<ManagementBaseObject>()
                .Where(service => (string)service.GetPropertyValue("Name") == ServiceName)
                .Select(service => (string)service.GetPropertyValue("Name"));

            return collection.Any();
        }

        public static string GetServiceNameFromPath(string path)
        {
            using var searcher = new ManagementObjectSearcher("root\\cimv2", "SELECT Name,PathName from Win32_Service");

            var collection = searcher.Get().Cast<ManagementBaseObject>()
                .Where(service => ((string)service.GetPropertyValue("PathName")).Contains(path))
                .Select(service => (string)service.GetPropertyValue("Name"));

            return collection.FirstOrDefault(string.Empty);
        }

        public static string GetPathOfService(string ServiceName)
        {
            using var searcher = new ManagementObjectSearcher("root\\cimv2", "SELECT Name,PathName from Win32_Service");

            var collection = searcher.Get().Cast<ManagementBaseObject>()
                .Where(service => (string)service.GetPropertyValue("Name") == ServiceName)
                .Select(service => (string)service.GetPropertyValue("PathName"));

            return collection.FirstOrDefault(string.Empty);
        }

        public static string GetServiceNameFromPath()
        {
            return GetServiceNameFromPath(ServicePath);
        }

        private static readonly string serviceFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DBADashService.exe");
        public static readonly string ServicePath = serviceFolder;

        public enum StartMode
        {
            Automatic,
            AutomaticDelayedStart,
            Manual,
            Disabled
        }

        public static string GetServiceInstallArgs(string serviceName, string userName, string password, StartMode mode = StartMode.AutomaticDelayedStart)
        {
            var modeString = mode switch
            {
                StartMode.Automatic => "auto",
                StartMode.AutomaticDelayedStart => "delayed-auto",
                StartMode.Manual => "demand",
                StartMode.Disabled => "disabled",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
            var args = $"create {serviceName} binpath=\"{ServicePath}\" start={modeString}";
            if (!string.IsNullOrEmpty(userName))
            {
                args += $" obj=\"{userName}\"";
            }

            if (!string.IsNullOrEmpty(password))
            {
                args += $" password=\"{password}\"";
            }
            return args;
        }

        public static bool IsServiceRunning(string serviceName)
        {
            var svcCtrl = ServiceController.GetServices()
                .FirstOrDefault(s => s.ServiceName == serviceName);
            return svcCtrl?.Status == ServiceControllerStatus.Running;
        }

        public static ServiceControllerStatus? StopService(string serviceName, int waitSeconds = 30)
        {
            if (!IsAdministrator)
            {
                throw new Exception("You must run this as an administrator");
            }
            var svcCtrl = ServiceController.GetServices()
                .FirstOrDefault(s => s.ServiceName == serviceName);
            if (svcCtrl?.Status == ServiceControllerStatus.Running)
            {
                Log.Information($"Stopping service {serviceName}");
                svcCtrl.Stop();
                svcCtrl.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(waitSeconds));
                svcCtrl.Refresh();
            }
            Log.Information($"Service {serviceName} is {svcCtrl?.Status}");
            return svcCtrl?.Status;
        }

        public static ServiceControllerStatus? StartService(string serviceName, int waitSeconds = 30)
        {
            if (!IsAdministrator)
            {
                throw new Exception("You must run this as an administrator");
            }
            var svcCtrl = ServiceController.GetServices()
                .FirstOrDefault(s => s.ServiceName == serviceName);
            if (svcCtrl?.Status == ServiceControllerStatus.Stopped)
            {
                Log.Information($"Starting service {serviceName}");
                svcCtrl.Start();
                svcCtrl.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(waitSeconds));
                svcCtrl.Refresh();
            }
            Log.Information($"Service {serviceName} is {svcCtrl?.Status}");
            return svcCtrl?.Status;
        }

        public static SCCommandResult UninstallService(string serviceName)
        {
            var outputBuilder = new StringBuilder();
            var result = new SCCommandResult();
            var path = GetPathOfService(serviceName);
            if (string.IsNullOrEmpty(path))
            {
                throw new Exception($"Service {serviceName} is not installed");
            }
            if (!path.Contains(ServicePath, StringComparison.CurrentCultureIgnoreCase)) // path might also include -displayname and -servicename parameters if installed by Topshelf
            {
                throw new Exception($"Service {serviceName} appears to be installed in a different path.  \nExpected: {ServicePath}.  \nActual: {path}");
            }
            if (!IsAdministrator)
            {
                throw new Exception("You must run this as an administrator");
            }
            if (StopService(serviceName) != ServiceControllerStatus.Stopped)
            {
                throw new Exception($"Service {serviceName} could not be stopped");
            }
            using Process p = new();
            var args = $"delete {serviceName}";
            Log.Information($"sc.exe {args}");
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            p.StartInfo = psi;
            p.OutputDataReceived += (sender, args) =>
            {
                var data = args.Data;
                Console.WriteLine(data);
                outputBuilder.AppendLine(data);
            };
            p.Start();
            p.BeginOutputReadLine();
            p?.WaitForExit();
            result.ExitCode = p.ExitCode;
            result.Output = outputBuilder.ToString();
            return result;
        }

        public static bool IsAdministrator =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

        public class SCCommandResult
        {
            public bool Success => ExitCode == 0;
            public string Output { get; set; }
            public int ExitCode { get; set; }
        }

        private static SCCommandResult SetServiceDescription(string serviceName, string description)
        {
            var outputBuilder = new StringBuilder();
            var result = new SCCommandResult();
            if (!IsAdministrator)
            {
                throw new Exception("You must run this as an administrator");
            }
            if (!IsServiceInstalledByName(serviceName))
            {
                throw new Exception($"Service {serviceName} is not installed");
            }
            using Process p = new();
            var args = $"description {serviceName} \"{description}\"";
            Log.Information($"sc.exe {args}");
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            p.StartInfo = psi;
            p.OutputDataReceived += (sender, args) =>
            {
                var data = args.Data;
                Console.WriteLine(data);
                outputBuilder.AppendLine(data);
            };
            p.Start();
            p.BeginOutputReadLine();
            p?.WaitForExit();
            result.ExitCode = p.ExitCode;
            result.Output = outputBuilder.ToString();
            return result;
        }

        private static SCCommandResult SetServiceRecovery(string serviceName)
        {
            var outputBuilder = new StringBuilder();
            var result = new SCCommandResult();
            if (!IsAdministrator)
            {
                throw new Exception("You must run this as an administrator");
            }
            if (!IsServiceInstalledByName(serviceName))
            {
                throw new Exception($"Service {serviceName} is not installed");
            }
            using Process p = new();
            var args = $"failure {serviceName} reset=0 actions=restart/60000/restart/60000//60000"; // Restart twice after 60 seconds, then take no action
            Log.Information($"sc.exe {args}");
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            p.StartInfo = psi;
            p.OutputDataReceived += (sender, args) =>
            {
                var data = args.Data;
                Console.WriteLine(data);
                outputBuilder.AppendLine(data);
            };
            p.Start();
            p.BeginOutputReadLine();
            p?.WaitForExit();
            result.ExitCode = p.ExitCode;
            result.Output = outputBuilder.ToString();
            return result;
        }

        public static SCCommandResult InstallService(string serviceName, string userName, string password,
            StartMode mode = StartMode.AutomaticDelayedStart)
        {
            var result = new SCCommandResult();
            var outputBuilder = new StringBuilder();
            if (IsServiceInstalledByName(serviceName))
            {
                throw new Exception($"Service {serviceName} is already installed");
            }
            if (!IsAdministrator)
            {
                throw new Exception("You must run this as an administrator");
            }

            using Process p = new();
            var args = GetServiceInstallArgs(serviceName, userName, password, mode);
            var DebugArgs = GetServiceInstallArgs(serviceName, userName, string.IsNullOrEmpty(password) ? string.Empty : "*****", mode);
            Log.Information($"sc.exe {DebugArgs}");
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            p.StartInfo = psi;
            p.OutputDataReceived += (sender, args) =>
            {
                var data = args.Data;
                Console.WriteLine(data);
                outputBuilder.AppendLine(data);
            };
            p.Start();
            p.BeginOutputReadLine();
            p?.WaitForExit();
            result.ExitCode = p.ExitCode;
            result.Output = outputBuilder.ToString();
            if (p.ExitCode == 0)
            {
                Log.Information("Setting service description");
                var descriptionResult = SetServiceDescription(serviceName, "Monitoring tool for SQL Server.  https://dbadash.com");
                if (!descriptionResult.Success)
                {
                    Log.Error("Error setting service description: {result}", descriptionResult.Output);
                    result.Output += "Error setting service description\n" + descriptionResult.Output;
                    result.ExitCode = descriptionResult.ExitCode;
                }
                Log.Information("Setting service recovery options");
                var recoveryResult = SetServiceRecovery(serviceName);
                if (!recoveryResult.Success)
                {
                    Log.Error("Error setting service recovery options: {result}", recoveryResult.Output);
                    result.Output += "Error setting service recovery options\n" + recoveryResult.Output;
                    result.ExitCode = recoveryResult.ExitCode;
                }
            }

            return result;
        }
    }
}