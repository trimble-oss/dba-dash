using DBADash;
using Octokit;
using Quartz;
using Serilog;
using System;
using System.Threading.Tasks;

namespace DBADashService
{
    /// <summary>
    /// Scheduled job that periodically checks for DBA Dash upgrades and initiates upgrade if available and permissions are valid.
    /// </summary>
    [DisallowConcurrentExecution]
    public class UpgradeCheckJob : IJob
    {
        private static readonly CollectionConfig config = SchedulerServiceConfig.Config;

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                Log.Information("Starting upgrade check");

                // Check if upgrade is already in progress
                if (Upgrade.IsUpgradeIncomplete)
                {
                    Log.Warning("Incomplete upgrade detected. Skipping upgrade check. {message}", Upgrade.IncompleteUpgradeMessage);
                    return;
                }

                // Get the latest release from GitHub
                Release release;
                try
                {
                    release = await Upgrade.GetLatestVersionAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to check for latest version from GitHub");
                    return;
                }

                if (release == null)
                {
                    Log.Warning("Could not retrieve latest release information from GitHub");
                    return;
                }

                var currentVersion = Upgrade.CurrentVersion();
                var releaseVersion = new Version(release.TagName);

                Log.Information("Current version: {currentVersion}, Latest version: {latestVersion}", currentVersion, releaseVersion);

                // Check if upgrade is available
                if (!Upgrade.IsUpgradeAvailable(release))
                {
                    Log.Information("No upgrade available. Already running latest version.");
                    return;
                }

                Log.Information("Upgrade available: {version}", release.TagName);

                // Validate that the service account has the necessary permissions before attempting upgrade
                // ServiceTools is Windows-only; automatic upgrade is not supported on other platforms
                if (!OperatingSystem.IsWindows())
                {
                    Log.Warning("Automatic upgrade is only supported on Windows. Skipping upgrade.");
                    return;
                }
                if (!ServiceTools.CanServiceAccountPerformUpgrade(config))
                {
                    Log.Error("Service account does not have sufficient permissions to perform upgrade. Skipping upgrade.");
                    return;
                }

                Log.Information("Service account has necessary permissions. Initiating upgrade to {version}", release.TagName);

                // Trigger the upgrade
                try
                {
                    await Upgrade.UpgradeDBADashAsync(tag: release.TagName, startGUI: false, startConfig: false, noExit: false, nonInteractive: true);
                    Log.Information("Upgrade initiated successfully for version {version}", release.TagName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to initiate upgrade to {version}", release.TagName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in UpgradeCheckJob");
            }
        }
    }
}