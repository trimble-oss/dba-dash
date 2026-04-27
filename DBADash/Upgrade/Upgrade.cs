using Octokit;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

namespace DBADash
{
    public class Upgrade
    {
        public const string GITHUB_OWNER = "trimble-oss";
        public const string GITHUB_REPO = "dba-dash";
        public const string GITHUB_APP = "dba-dash";
        public const string GITHUB_BRANCH = "main";
        public const string GITHUB_UPGRADESCRIPT = "UpgradeDBADash.ps1";
        public static string GITHUB_UPGRADESCRIPTPATH => Path.Combine(ApplicationPath, GITHUB_UPGRADESCRIPT);
        public const string GITHUB_SCRIPTPATH = "Scripts/";

        public const string AppURL = "http://dbadash.com";
        public const string AuthorURL = "https://github.com/DavidWiseman";
        public const string ServiceFileName = "DBADashService.exe";
        public const string UpgradeFile = "DBADash.Upgrade";
        public const string UpgradeLogFile = "DBADash.UpgradeLog.txt";
        public const string LocalScriptFile = "DBADash.LocalScript";
        public const string IncompleteUpgradeMessage = $"Incomplete upgrade of DBA Dash detected.  File '{UpgradeFile}' found in directory. Upgrade might have failed due to locked files. More info: https://dbadash.com/upgrades/";

        public static bool IsUpgradeIncomplete => File.Exists(Path.Combine(ApplicationPath, UpgradeFile));

        /// <summary>
        /// When this file is present in the application directory, the local upgrade script is used instead of
        /// downloading the latest from GitHub. Useful for testing local script changes.
        /// </summary>
        public static bool UseLocalScript => File.Exists(Path.Combine(ApplicationPath, LocalScriptFile));

        public enum DeploymentTypes
        {
            GUI,
            ServiceAndGUI
        }

        /// <summary>Get latest release from github</summary>
        public static async Task<Release> GetLatestVersionAsync()
        {
            var client = new GitHubClient(new ProductHeaderValue(GITHUB_APP));
            return await client.Repository.Release.GetLatest(GITHUB_OWNER, GITHUB_REPO);
        }

        public static bool IsUpgradeAvailable(Release release)
        {
            var releaseVersion = new Version(release.TagName);
            return (CurrentVersion().CompareTo(releaseVersion) < 0);
        }

        public static Version CurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        public static async Task UpgradeDBADashAsync(string tag = "", bool startGUI = false, bool startConfig = false, bool noExit = true, bool nonInteractive = false)
        {
            if (!UseLocalScript)
            {
                var client = new GitHubClient(new ProductHeaderValue(GITHUB_APP));
                var upgradeScript = await client.Repository.Content.GetRawContentByRef(GITHUB_OWNER, GITHUB_REPO, GITHUB_SCRIPTPATH + GITHUB_UPGRADESCRIPT, GITHUB_BRANCH);
                await File.WriteAllBytesAsync(GITHUB_UPGRADESCRIPTPATH, upgradeScript);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Local upgrade script override active ({LocalScriptFile}). Skipping GitHub download.");
            }

            var logPath = Path.Combine(ApplicationPath, UpgradeLogFile);
            // Escape single quotes for PowerShell single-quoted strings ('' is the PS escape sequence for ').
            var psLogPath = logPath.Replace("'", "''");
            var psAppPath = ApplicationPath.Replace("'", "''");
            // Note: Setting working directory via ProcessStartInfo doesn't work when using "runas" verb.
            // -NonInteractive is passed to both powershell.exe and the script itself.
            var arguments = "-NoProfile " + (nonInteractive ? "-NonInteractive " : "") + (noExit ? "-NoExit " : "") + "-ExecutionPolicy ByPass -Command &{" +
                               $"Start-Transcript -Path '{psLogPath}'; " +
                               $"Set-Location -Path '{psAppPath}'; " +
                               "try { " +
                               $"./{GITHUB_UPGRADESCRIPT}" +
                                            (tag == string.Empty ? string.Empty : " -Tag " + tag)
                                            + (startGUI ? " -StartGUI" : string.Empty)
                                            + (startConfig ? " -StartConfig" : string.Empty)
                                            + (nonInteractive ? " -NonInteractive" : string.Empty)
                                            + $" -Repo \"{GITHUB_OWNER}/{GITHUB_REPO}\""
                                            + " } catch { Write-Error $_ } finally { Stop-Transcript }}";

            // Non-interactive (service) path: already elevated, no UAC prompt available.
            // Interactive (GUI) path: elevate via UAC so the script can stop/start the service.
            // Either way, fire-and-forget — the script stops this process as part of its work.
            Process.Start(new ProcessStartInfo()
            {
                FileName = "PowerShell.exe",
                Arguments = arguments,
                UseShellExecute = !nonInteractive,
                Verb = nonInteractive ? string.Empty : "runas",
                CreateNoWindow = nonInteractive
            });
        }

        public static string ApplicationPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static DeploymentTypes DeploymentType => File.Exists(Path.Combine(ApplicationPath, ServiceFileName)) ? DeploymentTypes.ServiceAndGUI : DeploymentTypes.GUI;

        public static string LatestVersionLink => $"https://github.com/{GITHUB_OWNER}/{GITHUB_REPO}/releases/latest";

        public static bool IsAdministrator => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);
    }
}