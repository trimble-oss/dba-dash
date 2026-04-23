using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;

namespace DBADash
{
    [SupportedOSPlatform("windows")]
    public static class ServiceTools
    {
        public static bool IsServiceInstalledByPath()
        {
            return GetServiceInfoFromPath(ServicePath) != null;
        }

        public static bool IsServiceInstalledByName(string serviceName)
        {
            return GetServiceInfoFromName(serviceName) != null;
        }

        private static string EscapeWqlString(string value)
        {
            // Escape single quotes for WQL by doubling them.
            return value?.Replace("'", "''");
        }

        private static string EscapeWqlLikeString(string value)
        {
            if (value == null) return null;
            // Escape WQL string delimiters and LIKE wildcard characters so the
            // supplied value is treated as a literal substring match.
            return EscapeWqlString(value)
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]")
                .Replace(@"\", @"\\");
        }

        public static ServiceInfo GetServiceInfoFromName(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName)) return null;
            var escapedServiceName = EscapeWqlString(serviceName);
            var query = $"SELECT Name,PathName,StartName from Win32_Service WHERE name = '{escapedServiceName}'";
            using var searcher = new ManagementObjectSearcher("root\\cimv2", query);

            var service = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();

            if (service == null) return null;

            var name = service.GetPropertyValue("Name") as string;
            var pathName = service.GetPropertyValue("PathName") as string;
            var account = service.GetPropertyValue("StartName") as string;

            return new ServiceInfo
            {
                Name = name,
                PathName = pathName,
                AccountName = account
            };
        }

        public static ServiceInfo GetServiceInfoFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var escapedPath = EscapeWqlLikeString(path);
            var query = $"SELECT Name,PathName,StartName from Win32_Service WHERE PathName LIKE '%{escapedPath}%'";
            using var searcher = new ManagementObjectSearcher("root\\cimv2", query);

            var service = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();

            if (service == null) return null;

            var name = service.GetPropertyValue("Name") as string;
            var pathName = service.GetPropertyValue("PathName") as string;
            var account = service.GetPropertyValue("StartName") as string;

            return new ServiceInfo
            {
                Name = name,
                PathName = pathName,
                AccountName = account
            };
        }

        public static ServiceInfo GetServiceInfoFromPath() => GetServiceInfoFromPath(ServicePath);

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
            return GetServiceInstallArgs(serviceName, ServicePath, userName, password, mode);
        }

        public static string GetServiceInstallArgs(string serviceName, string binPath, string userName, string password, StartMode mode = StartMode.AutomaticDelayedStart)
        {
            var modeString = mode switch
            {
                StartMode.Automatic => "auto",
                StartMode.AutomaticDelayedStart => "delayed-auto",
                StartMode.Manual => "demand",
                StartMode.Disabled => "disabled",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
            var args = $"create {serviceName} binpath=\"{binPath}\" start={modeString}";
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
            var serviceInfo = GetServiceInfoFromName(serviceName);
            var path = serviceInfo?.PathName;
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
            p.OutputDataReceived += (sender, _args) =>
            {
                var data = _args.Data;
                Console.WriteLine(data);
                outputBuilder.AppendLine(data);
            };
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
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
            p.OutputDataReceived += (sender, _args) =>
            {
                var data = _args.Data;
                Console.WriteLine(data);
                outputBuilder.AppendLine(data);
            };
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
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
            p.OutputDataReceived += (sender, _args) =>
            {
                var data = _args.Data;
                Console.WriteLine(data);
                outputBuilder.AppendLine(data);
            };
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
            result.ExitCode = p.ExitCode;
            result.Output = outputBuilder.ToString();
            return result;
        }

        public static SCCommandResult InstallService(string serviceName, string userName, string password,
            StartMode mode = StartMode.AutomaticDelayedStart)
        {
            return InstallService(serviceName, ServicePath, "Monitoring tool for SQL Server.  https://dbadash.com", userName, password, mode);
        }

        public static SCCommandResult InstallService(string serviceName, string binPath, string description, string userName, string password,
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
            var args = GetServiceInstallArgs(serviceName, binPath, userName, password, mode);
            var DebugArgs = GetServiceInstallArgs(serviceName, binPath, userName, string.IsNullOrEmpty(password) ? string.Empty : "*****", mode);
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
            p.OutputDataReceived += (sender, _args) =>
            {
                var data = _args.Data;
                Console.WriteLine(data);
                outputBuilder.AppendLine(data);
            };
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
            result.ExitCode = p.ExitCode;
            result.Output = outputBuilder.ToString();
            if (p.ExitCode == 0)
            {
                Log.Information("Setting service description");
                var descriptionResult = SetServiceDescription(serviceName, description);
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