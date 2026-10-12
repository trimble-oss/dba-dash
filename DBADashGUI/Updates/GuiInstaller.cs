using DBADashGUI.Viewers;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Velopack;

namespace DBADashGUI.Updates
{
    /// <summary>
    /// Keeps a copy of the GUI installed by its setup program at the version of the repository database it connects to.
    ///
    /// The GUI has to match the repository, so unlike the Visualizer it never looks for the latest release: when it connects
    /// to a repository on another version it offers to install that one - an update when the repository has been upgraded,
    /// and a downgrade when the GUI is the newer of the two.  A copy extracted from the zip is upgraded by the upgrade script,
    /// as before; none of this applies to it.
    /// </summary>
    internal static class GuiInstaller
    {
        private const string Repository = "https://github.com/trimble-oss/dba-dash";
        private const string AppName = "DBA Dash";

        /// <summary>
        /// Set to a folder that holds a built feed - what Scripts/Pack-Installer.ps1 writes - or to the URL of one, to try an
        /// installer release before it is published.
        /// </summary>
        public const string FeedOverrideVariable = "DBADASH_GUI_UPDATE_FEED";

        private static readonly Lazy<bool> Installed = new(() =>
        {
            try
            {
                // The source isn't read - no request is made until an update is looked for.
                return new UpdateManager(Repository).IsInstalled;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to tell whether the GUI was installed by its setup program");
                return false;
            }
        });

        /// <summary>True for a copy installed by the setup program, which updates itself.  Otherwise it was extracted from a zip.</summary>
        public static bool IsInstalled => Installed.Value;

        /// <summary>What happened when the GUI offered to install the repository's version.</summary>
        public enum Outcome
        {
            /// <summary>The version is being installed, and the GUI is closing to let it.</summary>
            Installing,

            /// <summary>The user said not now.</summary>
            Declined,

            /// <summary>There is no installer release for that version, or it couldn't be downloaded.  The user has been told.</summary>
            Unavailable
        }

        /// <summary>
        /// Offers to install the repository's version, <paramref name="repositoryVersion"/>, in place of
        /// <paramref name="current"/>.  On <see cref="Outcome.Installing"/> the GUI restarts at the new version once this
        /// process has ended, so the caller should exit.  Call from the UI thread.
        /// </summary>
        public static async Task<Outcome> OfferRepositoryVersionAsync(IWin32Window owner, Version current, Version repositoryVersion)
        {
            var target = new Version(repositoryVersion.Major, repositoryVersion.Minor, Math.Max(repositoryVersion.Build, 0));
            var running = new Version(current.Major, current.Minor, Math.Max(current.Build, 0));
            var downgrade = target < running;

            UpdateManager manager = null;
            UpdateInfo info;
            try
            {
                var source = RepositoryVersionSource.For(Repository, target, Environment.GetEnvironmentVariable(FeedOverrideVariable));
                manager = new UpdateManager(source, new UpdateOptions { AllowVersionDowngrade = true });
                info = await manager.CheckForUpdatesAsync();
            }
            catch (Exception ex)
            {
                // A release without an installer has no feed: GitHub answers 404.
                Log.Warning(ex, "Unable to find the installer for version {version}", target);
                info = null;
            }

            if (info == null)
            {
                Show(owner, new TaskDialogPage
                {
                    Caption = AppName,
                    Heading = $"Version {target} isn't available to install",
                    Text = $"The repository database is on version {target} and this is {running}.  No installer for {target} " +
                           $"could be found.\n\nYou can download it from {Repository}/releases.",
                    Icon = TaskDialogIcon.Warning,
                    Buttons = { TaskDialogButton.OK }
                });
                return Outcome.Unavailable;
            }

            var adminNote = InstallScope.IsForAllUsers(AppContext.BaseDirectory, manager.AppId) ? "  " + InstallScope.AdminNote : string.Empty;
            var install = new TaskDialogButton(downgrade ? $"Install {target}" : "Update Now");
            var notNow = new TaskDialogButton("Not Now");
            var page = new TaskDialogPage
            {
                Caption = AppName,
                Heading = downgrade
                    ? $"This version of DBA Dash is newer than the repository database"
                    : $"The repository database has been upgraded to {target}",
                Text = downgrade
                    ? $"You are running {running}, and the repository database is on {target}.  The GUI is designed for the " +
                      $"repository version it was released with.\n\nInstall {target}?  DBA Dash restarts to install it." + adminNote
                    : $"You are running {running}.  The GUI is designed for the repository version it was released with.\n\n" +
                      "DBA Dash restarts to install the update." + adminNote,
                Icon = downgrade ? TaskDialogIcon.Warning : TaskDialogIcon.Information,
                Buttons = { install, notNow },
                DefaultButton = install,
                AllowCancel = true
            };

            if (Show(owner, page) != install) return Outcome.Declined;

            if (!UpdateDownloadDialog.Download(owner, manager, info, AppName, $"Downloading version {target}",
                    "DBA Dash restarts when the download is complete.", out var failure))
            {
                if (failure == null) return Outcome.Declined; // Cancelled

                Log.Warning(failure, "Unable to download version {version}", target);
                CommonShared.ShowExceptionDialog(failure, "Unable to download the update",
                    text: $"You can download the setup program from {Repository}/releases instead.");
                return Outcome.Unavailable;
            }

            try
            {
                // Waits for this process to end, installs, and starts the new version with the same arguments.
                manager.WaitExitThenApplyUpdates(info.TargetFullRelease, silent: false, restart: true,
                    restartArgs: Environment.GetCommandLineArgs().Skip(1).ToArray());
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to install version {version}", target);
                CommonShared.ShowExceptionDialog(ex, "Unable to install the update");
                return Outcome.Unavailable;
            }

            return Outcome.Installing;
        }

        private static TaskDialogButton Show(IWin32Window owner, TaskDialogPage page) =>
            owner != null ? TaskDialog.ShowDialog(owner, page) : TaskDialog.ShowDialog(page);
    }
}
