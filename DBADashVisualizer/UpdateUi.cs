using DBADashGUI.Viewers;
using DBADashSharedGUI;
using Serilog;
using System.Net.Http;
using Velopack;
using Velopack.Sources;

namespace DBADashVisualizer
{
    /// <summary>
    /// The About box and the update prompts.  What an update means depends on how the app was installed:
    ///
    /// - from the setup program: the app downloads it itself, and installs it when the app is next closed;
    /// - with winget: winget updates it, so the prompt gives the command;
    /// - from the zip: there is a newer zip to download and extract over the old one.
    /// </summary>
    internal sealed class UpdateUi
    {
        private const string Website = "https://dbadash.com";
        private const string Repository = "https://github.com/trimble-oss/dba-dash";

        /// <summary>The package's identifier in winget, for the command that updates a copy installed by it.</summary>
        internal const string WingetPackageId = "Trimble.DBADashVisualizer";

        private static string WingetUpgradeCommand => $"winget upgrade {WingetPackageId}";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

        private readonly UpdateService _service;
        private readonly string _appName;

        /// <summary>Velopack, which installs and updates a copy installed by the setup program.</summary>
        private readonly UpdateManager _installer;

        /// <summary>What Velopack found the last time it looked, for downloading.  Null when there is nothing newer.</summary>
        private UpdateInfo _pending;

        /// <summary>How this copy was installed, which decides what an update means.</summary>
        private readonly InstallSource _source;

        /// <summary>The version running - 4.19.0.</summary>
        internal static Version CurrentVersion
        {
            get
            {
                var version = typeof(UpdateUi).Assembly.GetName().Version ?? new Version();
                return new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
            }
        }

        internal UpdateUi(string appName, IViewerSettingsStore store)
        {
            _appName = appName;
            _installer = CreateInstallerManager();
            _source = _installer.IsInstalled ? InstallSource.Installer : InstallLocation.Detect(AppContext.BaseDirectory);

            Func<CancellationToken, Task<ReleaseInfo>> getLatest;
            if (_source == InstallSource.Installer)
            {
                getLatest = _ => LatestFromInstallerAsync();
            }
            else
            {
                getLatest = new ReleaseChecker(Http, "trimble-oss", "dba-dash", "DBADash_Visualizer_").GetLatestAsync;
            }

            _service = new UpdateService(store, getLatest, CurrentVersion);
        }

        /// <summary>
        /// Velopack reads the feed the release process publishes with each release - a releases.win.json and the packages
        /// it lists - from the repository's GitHub releases.
        /// </summary>
        private static UpdateManager CreateInstallerManager()
        {
            // Setting this to a folder that holds a built feed - what Scripts/Pack-VisualizerInstaller.ps1 writes - or to the
            // URL of one, tries an installer release before it is published.
            var feed = Environment.GetEnvironmentVariable("DBADASH_VISUALIZER_UPDATE_FEED");
            if (!string.IsNullOrWhiteSpace(feed)) return new UpdateManager(feed);

            return new UpdateManager(new GithubSource(Repository, accessToken: null, prerelease: false));
        }

        /// <summary>Asks Velopack whether there is a newer release, and remembers what it found for the download.</summary>
        private async Task<ReleaseInfo> LatestFromInstallerAsync()
        {
            _pending = await _installer.CheckForUpdatesAsync();
            if (_pending == null) return null;

            var target = _pending.TargetFullRelease.Version;
            var version = new Version(target.Major, target.Minor, target.Patch);
            return new ReleaseInfo(version, $"{Repository}/releases/tag/{version}", DownloadUrl: null);
        }

        /// <summary>The window in front, for a dialog to belong to.  Null when there isn't one.</summary>
        private static Form Owner =>
            Form.ActiveForm ?? Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Visible && !f.IsDisposed);

        /// <summary>
        /// Looks for an update once the app is up and idle, so a plan opened from Explorer isn't held back by it, and
        /// tells the user only if there is one.  Call from the UI thread.
        /// </summary>
        internal void CheckAtStartUp()
        {
            void OnIdle(object sender, EventArgs e)
            {
                Application.Idle -= OnIdle;
                _ = CheckAtStartUpAsync();
            }

            Application.Idle += OnIdle;
        }

        private async Task CheckAtStartUpAsync()
        {
            try
            {
                // The viewer first, and the question after it.
                await Task.Delay(TimeSpan.FromSeconds(3));

                var release = await _service.CheckIfDueAsync();
                if (release != null) Offer(release);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Update check failed");
            }
        }

        /// <summary>The Start menu, Check for Updates and About items for the viewers' Settings menus.</summary>
        internal IEnumerable<Func<ToolStripItem>> MenuItems()
        {
            // The setup program puts its own shortcut in the Start menu, and takes it away again.
            if (_source != InstallSource.Installer) yield return StartMenuItem;

            yield return () => new ToolStripMenuItem("Check for Updates...", null, async (_, _) => await CheckNowAsync());
            yield return () => new ToolStripMenuItem($"About {_appName}...", null, (_, _) => ShowAbout());
        }

        /// <summary>
        /// Adds or removes the Start menu shortcut.  Ticked when there is one, read each time the menu opens as it can be
        /// changed from the command line, or by deleting it.
        /// </summary>
        private static ToolStripItem StartMenuItem()
        {
            var item = new ToolStripMenuItem("Show in Start Menu");
            item.Click += (_, _) => ToggleStartMenuShortcut();

            void Refresh()
            {
                item.Checked = StartMenuShortcut.Exists;
                item.ToolTipText = StartMenuShortcut.Exists && !StartMenuShortcut.PointsAtThisCopy
                    ? "The shortcut is for another copy.  Click to remove it."
                    : "Adds DBA Dash Visualizer to your Start menu, or removes it from there.";
            }

            // The menu that will hold the item isn't known until it is added to one.
            item.OwnerChanged += (_, _) =>
            {
                if (item.Owner is ToolStripDropDown { OwnerItem: ToolStripDropDownItem parent })
                {
                    parent.DropDownOpening += (_, _) => Refresh();
                }
            };
            Refresh();
            return item;
        }

        private static void ToggleStartMenuShortcut()
        {
            try
            {
                // Ticked means there is a shortcut, so clicking removes it - whichever copy it points at.
                if (StartMenuShortcut.Exists)
                {
                    StartMenuShortcut.Remove();
                }
                else
                {
                    StartMenuShortcut.Create();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to change the Start menu shortcut");
                CommonShared.ShowExceptionDialog(ex, "Unable to change the Start menu shortcut");
            }
        }

        private async Task CheckNowAsync()
        {
            try
            {
                var release = await _service.CheckNowAsync();
                if (release != null)
                {
                    Offer(release);
                    return;
                }

                Show(new TaskDialogPage
                {
                    Caption = _appName,
                    Heading = "You have the latest version",
                    Text = $"{_appName} {CurrentVersion} is up to date.",
                    Icon = TaskDialogIcon.Information,
                    Buttons = { TaskDialogButton.OK }
                });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Update check failed");
                CommonShared.ShowExceptionDialog(ex, "Unable to check for updates",
                    text: $"GitHub could not be reached.  You can look for a newer version at {Repository}/releases.");
            }
        }

        private void Offer(ReleaseInfo release)
        {
            var viaWinget = _source == InstallSource.Winget;
            var viaSetup = _source == InstallSource.Installer && _pending != null;

            // The first button does the update, or as much of it as this app can.
            var primary = new TaskDialogButton(viaSetup ? "Install Update" : viaWinget ? "Copy Command" : "Download");
            var notes = new TaskDialogButton("Release Notes") { AllowCloseDialog = false };
            var skip = new TaskDialogButton("Skip This Version");
            var later = new TaskDialogButton("Remind Me Later");

            notes.Click += (_, _) => Open(release.ReleaseUrl);

            string text;
            if (viaSetup)
            {
                text = $"You are running {CurrentVersion}.\n\nThe update is downloaded now and installed when you close {_appName}.";
            }
            else if (viaWinget)
            {
                text = $"You are running {CurrentVersion}, installed with winget.\n\nTo update, run:\n{WingetUpgradeCommand}\n\n" +
                       "A new release can take a little while to reach winget - if it says there is nothing to upgrade, try again later.";
            }
            else
            {
                text = $"You are running {CurrentVersion}.\n\nDownload the zip and extract it over this folder to update.";
            }

            var page = new TaskDialogPage
            {
                Caption = _appName,
                Heading = $"Version {release.Version} is available",
                Text = text,
                Icon = TaskDialogIcon.Information,
                Buttons = { primary, notes, skip, later },
                DefaultButton = primary,
                AllowCancel = true
            };

            var result = Show(page);
            if (result == primary)
            {
                if (viaSetup)
                {
                    InstallUpdate(release);
                }
                else if (viaWinget)
                {
                    CopyToClipboard(WingetUpgradeCommand);
                }
                else
                {
                    Open(release.DownloadUrl);
                }
            }
            else if (result == skip)
            {
                _service.Skip(release);
            }
        }

        /// <summary>
        /// Downloads the update, with a progress bar the user can cancel, and arranges for it to be installed when the app
        /// closes.  It isn't applied straight away: that would close the windows the user has open, and the plan or graph in
        /// them, to restart with none.
        /// </summary>
        private void InstallUpdate(ReleaseInfo release)
        {
            var info = _pending;
            if (info == null) return;

            var cancel = TaskDialogButton.Cancel;
            var bar = new TaskDialogProgressBar { Minimum = 0, Maximum = 100 };
            using var cancellation = new CancellationTokenSource();
            var completed = false;
            Exception failure = null;
            var ui = SynchronizationContext.Current;

            var page = new TaskDialogPage
            {
                Caption = _appName,
                Heading = $"Downloading version {release.Version}",
                Text = "The update is installed when you close the app.",
                ProgressBar = bar,
                Buttons = { cancel },
                AllowCancel = true
            };

            cancel.Click += (_, _) => cancellation.Cancel();
            page.Created += async (_, _) =>
            {
                try
                {
                    // Progress is reported from the download's thread, and the dialog belongs to this one.
                    await _installer.DownloadUpdatesAsync(info,
                        percent => ui?.Post(_ => bar.Value = Math.Clamp(percent, 0, 100), null),
                        cancelToken: cancellation.Token);
                    completed = true;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    // Closes the dialog: the button's own answer isn't wanted.
                    if (!cancellation.IsCancellationRequested) cancel.PerformClick();
                }
            };

            Show(page);

            if (failure != null)
            {
                Log.Warning(failure, "Unable to download the update");
                CommonShared.ShowExceptionDialog(failure, "Unable to download the update",
                    text: $"You can download the setup program from {Repository}/releases instead.");
                return;
            }

            if (!completed) return;

            try
            {
                // Waits for this process to end, then installs, and doesn't start it again.
                _installer.WaitExitThenApplyUpdates(info.TargetFullRelease, silent: true, restart: false);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to schedule the update");
                CommonShared.ShowExceptionDialog(ex, "Unable to install the update");
                return;
            }

            Show(new TaskDialogPage
            {
                Caption = _appName,
                Heading = "The update is ready",
                Text = $"Version {release.Version} is installed when you close {_appName}.",
                Icon = TaskDialogIcon.Information,
                Buttons = { TaskDialogButton.OK }
            });
        }

        private void ShowAbout()
        {
            var check = new TaskDialogButton("Check for Updates");
            var website = new TaskDialogButton("Website");
            var autoCheck = new TaskDialogVerificationCheckBox("Check for updates automatically", _service.AutoCheck);

            var installedNote = _source switch
            {
                InstallSource.Winget => "Installed with winget - update it with winget upgrade.\n\n",
                InstallSource.Installer => "Installed with the setup program - updates are installed for you.\n\n",
                _ => string.Empty
            };

            var page = new TaskDialogPage
            {
                Caption = $"About {_appName}",
                Heading = _appName,
                Text = $"Version {CurrentVersion}\n\n" +
                       "Opens SQL Server execution plans (.sqlplan) and deadlock graphs (.xdl).  Part of DBA Dash, " +
                       "an open source SQL Server monitoring tool.\n\n" +
                       installedNote +
                       "Copyright © Trimble, Inc.  Released under the MIT licence.",
                Icon = TaskDialogIcon.Information,
                Buttons = { check, website, TaskDialogButton.Close },
                DefaultButton = TaskDialogButton.Close,
                Verification = autoCheck,
                AllowCancel = true
            };

            // Website opens the page and leaves the box up; the others answer it.
            website.AllowCloseDialog = false;
            website.Click += (_, _) => Open(Website);

            var result = Show(page);
            if (autoCheck.Checked != _service.AutoCheck) _service.AutoCheck = autoCheck.Checked;
            if (result == check) _ = CheckNowAsync();
        }

        private static TaskDialogButton Show(TaskDialogPage page)
        {
            var owner = Owner;
            return owner != null ? TaskDialog.ShowDialog(owner, page) : TaskDialog.ShowDialog(page);
        }

        private static void CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                // Another app can hold the clipboard.  The command was on screen, so the user isn't left without it.
                Log.Warning(ex, "Unable to copy to the clipboard");
            }
        }

        private static void Open(string url)
        {
            try
            {
                CommonShared.OpenURL(url);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to open {url}", url);
            }
        }
    }
}
