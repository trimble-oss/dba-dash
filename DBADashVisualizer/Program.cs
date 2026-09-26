using DBADashGUI.ShellIntegration;
using DBADashSharedGUI;
using DBADashGUI.Theme;
using DBADashGUI.Viewers;
using Microsoft.Win32;
using Serilog;
using System.IO;
using System.Text;
using Velopack;

namespace DBADashVisualizer
{
    internal static class Program
    {
        private const string AppName = "DBA Dash Visualizer";

        private const string RegisterOption = "--RegisterFileAssociation";
        private const string UnregisterOption = "--UnregisterFileAssociation";
        private const string CreateShortcutOption = "--CreateStartMenuShortcut";
        private const string RemoveShortcutOption = "--RemoveStartMenuShortcut";

        /// <summary>Per user, as the app can be run from a folder the user can't write to.</summary>
        private static string DataFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBADash");

        private static string LogFolder => Path.Combine(DataFolder, "Logs");

        private static string SettingsPath => Path.Combine(DataFolder, "Visualizer", "settings.json");

        private static UpdateUi _updates;

        [STAThread]
        private static int Main(string[] args)
        {
            ViewerApp.Identity = new ViewerAppIdentity(
                AppName,
                "DBADashVisualizer",
                "Viewer for SQL Server execution plans (.sqlplan) and deadlock graphs (.xdl)",
                IsFullGui: false);

            // A copy from the setup program is started by its installer with a switch to install, update or uninstall it.
            // Velopack handles that and ends the process; for any other start it does nothing.  It has to come first, and
            // after the identity, which the clean-up on uninstall needs.
            VelopackApp.Build()
                .OnBeforeUninstallFastCallback(_ => RemoveRegistrations())
                .Run();

            ApplicationConfiguration.Initialize();
            ConfigureLogging();

            try
            {
                Application.ThreadException += (_, e) =>
                {
                    Log.Error(e.Exception, "Unhandled exception");
                    CommonShared.ShowExceptionDialog(e.Exception);
                };

                ViewerSettings.Store = new JsonFileSettingsStore(SettingsPath);
                ApplyTheme();

                _updates = new UpdateUi(AppName, ViewerSettings.Store);
                foreach (var item in _updates.MenuItems()) ViewerApp.SettingsMenuItems.Add(item);

                return Run(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unable to start {app}", AppName);
                CommonShared.ShowExceptionDialog(ex, $"Error starting {AppName}");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static int Run(string[] args)
        {
            if (args.Any(a => a is "-h" or "--help" or "-?" or "/?"))
            {
                MessageBox.Show(Usage, AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }

            if (args.Contains(RegisterOption, StringComparer.OrdinalIgnoreCase) ||
                args.Contains(UnregisterOption, StringComparer.OrdinalIgnoreCase))
            {
                return SetFileAssociation(args.Contains(RegisterOption, StringComparer.OrdinalIgnoreCase)) ? 0 : 1;
            }

            if (args.Contains(CreateShortcutOption, StringComparer.OrdinalIgnoreCase) ||
                args.Contains(RemoveShortcutOption, StringComparer.OrdinalIgnoreCase))
            {
                return SetStartMenuShortcut(args.Contains(CreateShortcutOption, StringComparer.OrdinalIgnoreCase)) ? 0 : 1;
            }

            // Anything that looks like a switch and isn't one is more likely a typo than a file with that name.
            var unknown = args.Where(a => a.StartsWith('-') && !File.Exists(a)).ToList();
            if (unknown.Count > 0)
            {
                MessageBox.Show($"Unknown option: {string.Join(" ", unknown)}\n\n{Usage}", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 1;
            }

            // Keeps an existing file association working when this copy has replaced the one it points at.  Never adds
            // one the user didn't ask for.
            FileAssociation.UpdateRegistrations();
            StartMenuShortcut.Repair();

            var files = args.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
            var missing = files.Where(f => !File.Exists(f)).ToList();
            if (missing.Count > 0)
            {
                MessageBox.Show($"File not found:\n{string.Join("\n", missing)}", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                files = files.Except(missing).ToList();
                if (files.Count == 0) return 1;
            }

            // Started with no file - from the Start menu, say - so ask for one.  Closing the dialog ends the app.
            if (files.Count == 0) files = ViewerApp.PromptForFiles().ToList();
            if (files.Count == 0) return 0;

            // Looks for a newer version once the viewer is up - at most once a day, and never if the user has switched it off.
            _updates.CheckAtStartUp();

            ViewerApp.Run(files);
            return 0;
        }

        private static string Usage =>
            $"{AppName} opens SQL Server execution plans (.sqlplan) and deadlock graphs (.xdl).\n\n" +
            $"DBADashVisualizer.exe [file ...]\n" +
            $"    Open the files.  With none, asks for one.\n\n" +
            $"{RegisterOption}\n" +
            $"    Offer {AppName} in Open with for .sqlplan and .xdl files (current user), then exit.\n\n" +
            $"{UnregisterOption}\n" +
            $"    Remove what {RegisterOption} added, then exit.\n\n" +
            $"{CreateShortcutOption}\n" +
            $"    Add {AppName} to your Start menu (current user), then exit.\n\n" +
            $"{RemoveShortcutOption}\n" +
            $"    Remove it from your Start menu, then exit.";

        /// <summary>
        /// On uninstall, takes out what the app added to the user's registry: the Open with entries for .sqlplan and .xdl,
        /// if they are for this copy.  The Start menu shortcut is the installer's, and it removes it.
        /// </summary>
        private static void RemoveRegistrations()
        {
            try
            {
                foreach (var association in FileAssociation.All)
                {
                    if (association.IsRegisteredToThisCopy) association.Unregister();
                }
            }
            catch (Exception)
            {
                // The uninstall carries on regardless: a stale Open with entry is harmless, a failed uninstall is not.
            }
        }

        private static bool SetStartMenuShortcut(bool create)
        {
            try
            {
                if (create)
                {
                    StartMenuShortcut.Create();
                }
                else
                {
                    StartMenuShortcut.Remove();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to update the Start menu shortcut");
                CommonShared.ShowExceptionDialog(ex, "Error updating the Start menu shortcut");
                return false;
            }
        }

        private static bool SetFileAssociation(bool register)
        {
            try
            {
                if (register)
                {
                    FileAssociation.RegisterAll();
                }
                else
                {
                    FileAssociation.UnregisterAll();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to update the file associations");
                CommonShared.ShowExceptionDialog(ex, "Error updating the file associations");
                return false;
            }
        }

        /// <summary>
        /// The theme is the one saved in the settings file ("Theme": "Dark", "White" or "Default"), and otherwise follows
        /// Windows - dark when apps are set to use the dark theme.
        /// </summary>
        private static void ApplyTheme()
        {
            try
            {
                var saved = ViewerSettings.Store.Get("Theme") as string;
                var type = Enum.TryParse(saved, ignoreCase: true, out ThemeType parsed)
                    ? parsed
                    : WindowsUsesDarkTheme() ? ThemeType.Dark : ThemeType.Default;

                ThemeExtensions.CurrentTheme = type switch
                {
                    ThemeType.Dark => new DarkTheme(),
                    ThemeType.White => new WhiteTheme(),
                    _ => new BaseTheme()
                };
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to apply the theme");
            }
        }

        private static bool WindowsUsesDarkTheme()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is 0;
        }

        /// <summary>Without this the log calls go nowhere.  A small rolling file, as the app has no console.</summary>
        private static void ConfigureLogging()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(Path.Combine(LogFolder, "DBADashVisualizer-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        // A copy per file Explorer opens can be running at once
                        shared: true,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {NewLine}{Exception}",
                        encoding: Encoding.UTF8)
                    .CreateLogger();
            }
            catch
            {
                // Logging is a diagnostic aid - never a reason not to start
            }
        }
    }
}
