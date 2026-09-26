using DBADashGUI.Viewers;
using DBADashSharedGUI;
using Serilog;
using System.Net.Http;

namespace DBADashVisualizer
{
    /// <summary>
    /// The About box and the update prompts.  Updating is a matter of downloading the new zip and unzipping it over the
    /// old one, so all this does is say there is one and take the user to it.
    /// </summary>
    internal sealed class UpdateUi
    {
        private const string Website = "https://dbadash.com";
        private const string Repository = "https://github.com/trimble-oss/dba-dash";

        /// <summary>The package's identifier in winget, for the command that updates a copy installed by it.</summary>
        internal const string WingetPackageId = "Trimble.DBADashVisualizer";

        /// <summary>How this copy was installed, which decides what an update means.</summary>
        private static readonly InstallSource Source = InstallLocation.Detect(AppContext.BaseDirectory);

        private static string WingetUpgradeCommand => $"winget upgrade {WingetPackageId}";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

        private readonly UpdateService _service;
        private readonly string _appName;

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
            var checker = new ReleaseChecker(Http, "trimble-oss", "dba-dash", "DBADash_Visualizer_");
            _service = new UpdateService(store, checker.GetLatestAsync, CurrentVersion);
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

        /// <summary>The Check for Updates and About items for the viewers' Settings menus.</summary>
        internal IEnumerable<Func<ToolStripItem>> MenuItems()
        {
            yield return StartMenuItem;
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
            // A copy installed by winget is updated by winget, not by extracting a zip over the folder it manages.
            var viaWinget = Source == InstallSource.Winget;
            var download = new TaskDialogButton(viaWinget ? "Copy Command" : "Download");
            var notes = new TaskDialogButton("Release Notes") { AllowCloseDialog = false };
            var skip = new TaskDialogButton("Skip This Version");
            var later = new TaskDialogButton("Remind Me Later");

            notes.Click += (_, _) => Open(release.ReleaseUrl);

            var page = new TaskDialogPage
            {
                Caption = _appName,
                Heading = $"Version {release.Version} is available",
                Text = viaWinget
                    ? $"You are running {CurrentVersion}, installed with winget.\n\nTo update, run:\n{WingetUpgradeCommand}\n\n" +
                      "A new release can take a little while to reach winget - if it says there is nothing to upgrade, try again later."
                    : $"You are running {CurrentVersion}.\n\nDownload the zip and extract it over this folder to update.",
                Icon = TaskDialogIcon.Information,
                Buttons = { download, notes, skip, later },
                DefaultButton = download,
                AllowCancel = true
            };

            var result = Show(page);
            if (result == download)
            {
                if (viaWinget)
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

        private void ShowAbout()
        {
            var check = new TaskDialogButton("Check for Updates");
            var website = new TaskDialogButton("Website");
            var autoCheck = new TaskDialogVerificationCheckBox("Check for updates automatically", _service.AutoCheck);

            var page = new TaskDialogPage
            {
                Caption = $"About {_appName}",
                Heading = _appName,
                Text = $"Version {CurrentVersion}\n\n" +
                       "Opens SQL Server execution plans (.sqlplan) and deadlock graphs (.xdl).  Part of DBA Dash, " +
                       "an open source SQL Server monitoring tool.\n\n" +
                       (Source == InstallSource.Winget ? "Installed with winget - update it with winget upgrade.\n\n" : string.Empty) +
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
