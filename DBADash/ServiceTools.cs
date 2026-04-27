using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
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

        /// <summary>
        /// Checks if the current service account (the account running the DBA Dash service) has sufficient permissions to perform an upgrade.
        /// This validates that:
        /// 1. The service folder is accessible and writable
        /// 2. The service can be stopped and started
        /// 3. The service account has db_owner access to all SQL repository databases
        /// </summary>
        /// <param name="config">The collection configuration containing service name and destination connections</param>
        /// <returns>True if the service account has sufficient permissions, false otherwise</returns>
        public static bool CanServiceAccountPerformUpgrade(CollectionConfig config)
        {
            try
            {
                // Check if service folder is writable
                if (!CanWriteToServiceFolder())
                {
                    Log.Warning("Service account does not have write permissions to service folder");
                    return false;
                }

                // Check if service can be stopped and started (permissions to control the service)
                if (!CanControlService(config.ServiceName))
                {
                    Log.Warning("Service account does not have permissions to control the service (stop/start)");
                    return false;
                }

                // Check if any DBA Dash processes are running that cannot be terminated
                if (!CanTerminateRunningProcesses())
                {
                    Log.Warning("Service account does not have permissions to terminate one or more running DBA Dash processes");
                    return false;
                }

                // Check if the database principal in each connection string has CONTROL permission on all SQL repository databases (typically granted via db_owner)
                if (!CanAccessRepositoryDatabaseWithControlPermission(config.AllDestinations))
                {
                    Log.Warning("Database principal does not have CONTROL permission on all repository databases (typically granted via db_owner role)");
                    return false;
                }

                Log.Information("Service account has sufficient permissions for upgrade");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error validating service account upgrade permissions");
                return false;
            }
        }

        /// <summary>
        /// Checks if the service folder is writable by attempting to create and delete a test file.
        /// </summary>
        /// <returns>True if the service folder is writable, false otherwise</returns>
        private static bool CanWriteToServiceFolder()
        {
            try
            {
                var testFilePath = Path.Combine(Path.GetDirectoryName(ServicePath) ?? AppContext.BaseDirectory, $".upgrade-check-{Guid.NewGuid():N}");
                using (File.Create(testFilePath, 1, FileOptions.DeleteOnClose)) { }
                return true;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Service folder write test failed");
                return false;
            }
        }

        /// <summary>
        /// Checks if the service can be controlled (stopped and started) by attempting to open it
        /// with SERVICE_STOP access rights via the SCM. This accurately reflects whether the
        /// current account can stop the service, unlike reading ServiceController.Status which
        /// only requires SERVICE_QUERY_STATUS.
        /// </summary>
        /// <param name="serviceName">The name of the service to check, or null to use the service from path</param>
        /// <returns>True if the service account has stop permissions, false otherwise</returns>
        private static bool CanControlService(string serviceName = null)
        {
            try
            {
                ServiceInfo serviceInfo;

                if (!string.IsNullOrEmpty(serviceName))
                {
                    serviceInfo = GetServiceInfoFromName(serviceName);
                    if (serviceInfo == null)
                    {
                        Log.Warning("DBA Dash service {serviceName} not found", serviceName);
                        return false;
                    }
                }
                else
                {
                    serviceInfo = GetServiceInfoFromPath();
                    if (serviceInfo == null)
                    {
                        Log.Warning("DBA Dash service not found");
                        return false;
                    }
                }

                // Open the SCM and then the service with SERVICE_STOP access to verify actual stop permission
                var scm = NativeMethods.OpenSCManager(null, null, NativeMethods.SC_MANAGER_CONNECT);
                if (scm == IntPtr.Zero)
                {
                    Log.Warning("Could not open SCM to verify service stop permissions");
                    return false;
                }
                try
                {
                    var svc = NativeMethods.OpenService(scm, serviceInfo.Name, NativeMethods.SERVICE_STOP | NativeMethods.SERVICE_START);
                    if (svc == IntPtr.Zero)
                    {
                        Log.Warning("Service account does not have stop/start permissions on service {serviceName}", serviceInfo.Name);
                        return false;
                    }
                    NativeMethods.CloseServiceHandle(svc);
                }
                finally
                {
                    NativeMethods.CloseServiceHandle(scm);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Service control check failed");
                return false;
            }
        }

        private static class NativeMethods
        {
            internal const uint SC_MANAGER_CONNECT = 0x0001;
            internal const uint SERVICE_STOP = 0x0020;
            internal const uint SERVICE_START = 0x0010;
            internal const uint PROCESS_TERMINATE = 0x0001;

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseServiceHandle(IntPtr hSCObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr hObject);
        }

        private static readonly string[] UpgradeProcessNames = ["DBADash", "DBADashServiceConfigTool", "DBADashConfig", "DBADashService"];

        /// <summary>
        /// Checks whether any DBA Dash processes currently running from the service folder can be terminated
        /// by the current account. Processes not running from this installation path are ignored.
        /// </summary>
        private static bool CanTerminateRunningProcesses()
        {
            try
            {
                var serviceDir = (Path.GetDirectoryName(ServicePath) ?? AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                foreach (var name in UpgradeProcessNames)
                {
                    foreach (var proc in Process.GetProcessesByName(name))
                    {
                        try
                        {
                            string procPath = null;
                            try { procPath = proc.MainModule?.FileName; } catch { /* access denied reading path */ }

                            // Only care about processes running from this installation
                            if (procPath == null || !procPath.StartsWith(serviceDir, StringComparison.OrdinalIgnoreCase))
                                continue;

                            var handle = NativeMethods.OpenProcess(NativeMethods.PROCESS_TERMINATE, false, proc.Id);
                            if (handle == IntPtr.Zero)
                            {
                                Log.Warning("Cannot terminate process {name} (PID {pid}): insufficient permissions", name, proc.Id);
                                return false;
                            }
                            NativeMethods.CloseHandle(handle);
                        }
                        finally
                        {
                            proc.Dispose();
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Process termination permission check failed");
                return false;
            }
        }

        /// <summary>
        /// Checks if the database principal in each destination connection string has CONTROL permission
        /// on the repository database. For integrated-security connections this is the Windows service
        /// account; for SQL-auth connections it is the SQL login specified in the connection string.
        /// CONTROL permission is typically granted via the db_owner role, but other grants can satisfy it too.
        /// Non-SQL destinations are excluded from the check as they don't require this permission for upgrades.
        /// </summary>
        /// <param name="destinations">The list of destination connections</param>
        /// <returns>True if the database principal has CONTROL permission on all SQL destinations, false otherwise</returns>
        private static bool CanAccessRepositoryDatabaseWithControlPermission(List<DBADashConnection> destinations)
        {
            try
            {
                if (destinations == null || destinations.Count == 0)
                {
                    Log.Debug("No destination connections to validate");
                    return true; // No SQL destinations to check
                }

                // Filter to only SQL destinations - non-SQL destinations don't need CONTROL permission for upgrades
                var sqlDestinations = destinations.Where(d => d.Type == DBADashConnection.ConnectionType.SQL).ToList();

                if (sqlDestinations.Count == 0)
                {
                    Log.Debug("No SQL destination connections to validate");
                    return true; // No SQL destinations to check
                }

                bool allHaveAccess = true;

                foreach (var destination in sqlDestinations)
                {
                    using var cn = new SqlConnection(destination.ConnectionString);

                    cn.Open();

                    using var cmd = new SqlCommand("SELECT HAS_PERMS_BY_NAME(NULL, 'DATABASE', 'CONTROL')", cn);

                    var scalarResult = cmd.ExecuteScalar();

                    // HAS_PERMS_BY_NAME can return NULL if the permission check cannot be evaluated
                    // Treat NULL or DBNull as 0 (no permission) to avoid InvalidCastException
                    var result = scalarResult == null || scalarResult == DBNull.Value ? 0 : Convert.ToInt32(scalarResult);

                    if (result > 0)
                    {
                        Log.Information("Database principal has CONTROL permission (e.g. via db_owner) in repository database: {database}", destination.InitialCatalog());
                    }
                    else
                    {
                        Log.Warning("Database principal does not have CONTROL permission in repository database: {database}. Grant db_owner or equivalent CONTROL permission.", destination.InitialCatalog());
                        allHaveAccess = false;
                    }
                }

                return allHaveAccess;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking CONTROL permission on repository database");
                return false;
            }
        }
    }
}