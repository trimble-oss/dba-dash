using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.Setup.Configuration;
using Microsoft.Win32;

namespace DBADashGUI.ShellIntegration
{
    /// <summary>
    /// Installs the DBA Dash SSMS extension - a separate VSIX project (DBADash.SSMSExtension) released
    /// on its own cadence, since it's expected to change far less often than the rest of DBA Dash.
    /// Embedded into this assembly at build time (see DBADash.Viewers.GUI.csproj) so it can be offered
    /// from the settings menu without a separate download, but not guaranteed to be there - a local dev
    /// build of just this project works fine without the SSMS extension having been built first.
    /// </summary>
    public static class SsmsExtensionInstaller
    {
        private const string ResourceName = "DBADash.SSMSExtension.vsix";

        // The Identity/@Id from DBADash.SSMSExtension/source.extension.vsixmanifest - the extension's
        // stable identity, unlike its Version attribute which changes every release. Hardcoded rather
        // than read from the embedded .vsix at runtime: it hasn't changed since the extension was first
        // created and isn't expected to, and parsing it back out of a zip just to uninstall by name would
        // be more code than the constant it would produce.
        private const string ExtensionId = "DBADash.SSMSExtension.b7e6f4d1-2c3a-4e5f-9b8a-1d2e3f4a5b6c";

        // Where DBADash.SSMSExtension's own VisualizerLauncher falls back to looking for this app's exe
        // when it can't find it any other way (registered file association, or - for the standalone
        // Visualizer only - its known Velopack install path). Written here too, from the other direction:
        // matches VisualizerLauncher's OwnSettingsKey/OwnSettingsValue exactly, but duplicated rather than
        // shared, since that project can't be referenced from here (net472 there, a hard VSSDK
        // requirement, vs net10 here).
        private const string DiscoveryKey = @"Software\DBADash\SSMSExtension";
        private const string DiscoveryValue = "VisualizerPath";

        /// <summary>True when this build actually has the extension embedded.</summary>
        public static bool IsAvailable => GetResourceStream() != null;

        /// <summary>
        /// Extracts the embedded .vsix to a temp file and starts it - the same as double-clicking it in
        /// Explorer, which Windows hands to VSIXInstaller without this needing to know where that is.
        /// </summary>
        public static void Install()
        {
            using var resource = GetResourceStream() ??
                                  throw new InvalidOperationException("The SSMS extension isn't included in this build.");

            // A folder of its own each time rather than one fixed path: a VSIXInstaller still running from
            // an earlier click can hold the previous file open, which would make overwriting it fail.
            var folder = Path.Combine(Path.GetTempPath(), "DBADash_SSMSExtension_" + Path.GetRandomFileName());
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, ResourceName);
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                resource.CopyTo(file);
            }

            RememberOwnPath();

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
        }

        /// <summary>
        /// Records this app's own exe path for the SSMS extension to find - the app installing the
        /// extension already knows exactly where it's running from, so there's no reason to make the
        /// extension guess at it later, or ask the user to browse to it, when neither a registered file
        /// association nor (for the full GUI, which has no fixed install location) a known install path
        /// applies. Best effort: a failure here just means the extension falls back to asking the user
        /// the first time it can't find either app any other way, same as before this existed.
        /// </summary>
        private static void RememberOwnPath()
        {
            try
            {
                var exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                using var key = Registry.CurrentUser.CreateSubKey(DiscoveryKey);
                key?.SetValue(DiscoveryValue, exePath);
            }
            catch
            {
                // Best effort - see summary above.
            }
        }

        /// <summary>The product ID SSMS reports through the Setup Configuration API - confirmed live
        /// against an actual SSMS 22 install (Microsoft.VisualStudio.Setup.Configuration.SetupConfiguration
        /// -> ISetupInstance2.GetProduct().GetId()), not assumed from a naming convention.</summary>
        private const string SsmsProductId = "Microsoft.VisualStudio.Product.Ssms";

        /// <summary>
        /// Runs VSIXInstaller /uninstall against the extension's own identity - there's no file to
        /// double-click for that the way Install() has, so this needs the actual VSIXInstaller.exe
        /// rather than Install()'s "hand a .vsix to whatever opens .vsix files" shortcut. That shortcut
        /// doesn't work here: on at least one real machine, the registered "open" command for .vsix is
        /// devenv.exe, not VSIXInstaller - fine for opening (installing) a specific file, since devenv
        /// special-cases being handed one, but it doesn't understand /uninstall:&lt;id&gt; at all and
        /// just reports it as a file it can't find. Runs with its own UI rather than /quiet, so if it
        /// needs to ask something (e.g. more than one compatible SSMS install exists) the user sees it,
        /// the same as they would with Install().
        /// </summary>
        public static void Uninstall()
        {
            var vsixInstaller = FindVsixInstaller() ??
                                 throw new InvalidOperationException(
                                     "Could not find VSIXInstaller.exe - is a compatible version of SSMS installed?");

            Process.Start(new ProcessStartInfo(vsixInstaller, $"/uninstall:{ExtensionId}") { UseShellExecute = false })
                ?.Dispose();
        }

        /// <summary>
        /// Finds VSIXInstaller.exe under an actual installed SSMS - via the same official Setup
        /// Configuration API (used by every "vswhere"-style tool, and by VSIXInstaller's own trampoline
        /// internally) that reports an install's real path regardless of where the user put it, rather
        /// than assuming the default Program Files location. Falls back to guessing the default location,
        /// then to the .vsix file association, only if that API isn't available at all - it's a
        /// system-registered COM component every VS-family product installs, so this is a genuine
        /// "should never happen" fallback rather than the expected path.
        /// </summary>
        private static string FindVsixInstaller() =>
            FindVsixInstallerViaSetupApi()
            ?? FindVsixInstallerInDefaultLocations()
            ?? FileAssociation.AssocQueryExecutable(".vsix");

        private static string FindVsixInstallerViaSetupApi()
        {
            try
            {
                var configuration = (ISetupConfiguration)new SetupConfiguration();
                var enumerator = configuration.EnumInstances();
                var batch = new ISetupInstance[1];

                while (true)
                {
                    enumerator.Next(1, batch, out var fetched);
                    if (fetched == 0) return null;

                    if (batch[0] is not ISetupInstance2 instance) continue;
                    if (instance.GetProduct()?.GetId() != SsmsProductId) continue;

                    var candidate = Path.Combine(instance.GetInstallationPath(), "Common7", "IDE", "VSIXInstaller.exe");
                    if (File.Exists(candidate)) return candidate;
                }
            }
            catch
            {
                // The Setup Configuration COM component isn't registered - fall through to guessing.
                return null;
            }
        }

        /// <summary>Only reached if the Setup Configuration API above isn't available at all - a default
        /// Program Files layout is still a reasonable guess, just not one that copes with a custom install
        /// location the way the API does.</summary>
        private static string FindVsixInstallerInDefaultLocations()
        {
            foreach (var programFiles in new[]
                     {
                         Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                         Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                     })
            {
                if (string.IsNullOrEmpty(programFiles) || !Directory.Exists(programFiles)) continue;

                IEnumerable<string> ssmsDirs;
                try
                {
                    ssmsDirs = Directory.EnumerateDirectories(programFiles, "Microsoft SQL Server Management Studio *");
                }
                catch
                {
                    continue;
                }

                foreach (var ssmsDir in ssmsDirs)
                {
                    string found;
                    try
                    {
                        // A few levels deep - SSMS 18-20 have it directly under Common7\IDE, SSMS 21+
                        // under a Release or Preview subfolder first - rather than assuming which.
                        found = Directory.EnumerateFiles(ssmsDir, "VSIXInstaller.exe", SearchOption.AllDirectories)
                            .FirstOrDefault();
                    }
                    catch
                    {
                        continue;
                    }

                    if (found != null) return found;
                }
            }

            return null;
        }

        private static Stream GetResourceStream() =>
            typeof(SsmsExtensionInstaller).Assembly.GetManifestResourceStream(ResourceName);
    }
}
