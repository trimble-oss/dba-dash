using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// Finds and launches whichever DBA Dash app can open a plan, deadlock graph or result set, with it written
    /// to a temp file: the stand-alone DBADashVisualizer.exe, or the full DBADash.exe GUI someone may already
    /// have installed instead - both open a file the same way, with no repository connection needed
    /// (DBADashGUI's own Program.cs takes the same shortcut, straight to the viewer, for exactly this
    /// reason). Neither is assumed to be there; whichever is found first wins, and the choice is never
    /// hardcoded to just the one someone might not have.
    ///
    /// This extension can't reference either app's own code - they're different projects on a different
    /// .NET runtime (net472 here vs net10.0-windows there, a hard requirement of running in-process
    /// inside SSMS) - so it writes its own temp file and finds its own copy of the exe, matching DBA
    /// Dash's existing conventions rather than inventing new ones.
    /// </summary>
    internal static class VisualizerLauncher
    {
        private const string VisualizerExeName = "DBADashVisualizer.exe";
        private const string GuiExeName = "DBADash.exe";

        // The same registry contract DBADashGUI.ShellIntegration.FileAssociation already writes when the
        // user registers file associations from either app's own settings menu - reused here rather than
        // inventing a second way to record where either exe lives. Each app registers under its own
        // ProgIDs (see ViewerAppIdentity.RegistryName) so both can be offered side by side.
        private const string VisualizerQueryPlanProgIdCommandKey =
            @"Software\Classes\DBADashVisualizer.QueryPlan\shell\open\command";

        private const string VisualizerDeadlockProgIdCommandKey =
            @"Software\Classes\DBADashVisualizer.DeadlockGraph\shell\open\command";

        private const string GuiQueryPlanProgIdCommandKey =
            @"Software\Classes\DBADash.QueryPlan\shell\open\command";

        private const string GuiDeadlockProgIdCommandKey =
            @"Software\Classes\DBADash.DeadlockGraph\shell\open\command";

        // Where this extension remembers the exe's location once the user has located it once, for
        // when none of the above has ever been registered.
        private const string OwnSettingsKey = @"Software\DBADash\SSMSExtension";
        private const string OwnSettingsValue = "VisualizerPath";

        // This extension's own version, recorded under the same key just before each launch, so the app it hands
        // a file to can tell whether a newer extension is available (SsmsExtensionInstaller.NeedsUpdate).  Recorded
        // at launch rather than once when SSMS loads the extension: side-by-side SSMS installs can each have a
        // different version, and it's the one that just handed over a file that matters.
        private const string OwnVersionValue = "ExtensionVersion";

        public static void OpenPlan(string planXml)
        {
            Open(planXml, ".sqlplan", Encoding.Unicode); // SSMS itself writes .sqlplan as UTF-16.
        }

        public static void OpenDeadlock(string deadlockXml)
        {
            Open(deadlockXml, ".xdl", Encoding.UTF8);
        }

        /// <summary>
        /// Opens XML that isn't from one of SSMS's own graphical plan/deadlock controls - e.g. a plain
        /// XML results tab, such as sp_BlitzLock opens a deadlock graph in. Written with a generic .xml
        /// extension rather than guessing which one it is here: both apps' own file open already sniffs
        /// a bare .xml's content to pick the right viewer (ViewerApp.OpenFiles), so there's no reason to
        /// duplicate that logic in this net472 process instead of the app itself.
        /// </summary>
        public static void OpenXml(string xml)
        {
            Open(xml, ".xml", Encoding.UTF8);
        }

        /// <summary>
        /// Writes a result set read from SSMS's results grid (ResultsGridReader) to a temp file for
        /// <see cref="OpenFile"/>. DataTable XML with its schema, so the column types survive: it's one of the
        /// formats DBA Dash's own grid export saves and reads back (GridSerializer), and .NET Framework can write
        /// it with nothing extra. Gzipped - a result set can run to hundreds of MB of XML - and as .xml.gz rather
        /// than .xml, so the app opens it as a grid without having to tell it apart from a plan or deadlock
        /// graph saved as .xml. Doesn't touch the UI, so it can run on a background thread.
        ///
        /// Writing a large result set takes a while too, so <paramref name="cancellationToken"/> is checked on every
        /// write to the file - the wait dialog's Cancel stops it rather than being ignored until it's done - and the
        /// partial file is deleted.
        /// </summary>
        public static string SaveGridToTemp(DataTable table, CancellationToken cancellationToken)
        {
            return SaveToTemp(".xml.gz", stream =>
            {
                using var gzip = new GZipStream(new CancellableStream(stream, cancellationToken), CompressionLevel.Fastest);
                table.WriteXml(gzip, XmlWriteMode.WriteSchema);
            });
        }

        private static void Open(string xml, string extension, Encoding encoding)
        {
            OpenFile(SaveToTemp(extension, stream =>
            {
                using var writer = new StreamWriter(stream, encoding);
                writer.Write(xml);
            }));
        }

        /// <summary>Opens a temp file written by this class in whichever DBA Dash app is found, asking the user
        /// to locate one if none is. Must be called on the UI thread, as it may prompt.</summary>
        public static void OpenFile(string tempFile)
        {
            var exePath = FindDBADashExe();
            if (exePath != null && LaunchApp(exePath, tempFile)) return;

            var result = MessageBox.Show(
                "Neither DBA Dash Visualizer nor DBA Dash could be found.\n\n" +
                "Would you like to locate one? The path will be remembered for next time.",
                "DBA Dash Visualizer",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            var browsedPath = BrowseForApp();
            if (browsedPath == null) return;

            if (!LaunchApp(browsedPath, tempFile))
            {
                MessageBox.Show($"Could not launch DBA Dash from:\n{browsedPath}",
                    "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Writes the XML to a random-named file under %TEMP%. FileMode.CreateNew refuses to overwrite
        /// or follow a file already sitting at the path, closing the race a predictable name would
        /// leave open. Sweeps this extension's own stale temp files older than an hour on the way in,
        /// rather than deleting the file right after handing it to another process to open. A write that fails, or is
        /// cancelled, deletes its partial file.
        /// </summary>
        private static string SaveToTemp(string extension, Action<Stream> write)
        {
            SweepOldTempFiles();

            var fileName = "ssms_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + extension;
            var path = Path.Combine(Path.GetTempPath(), fileName);
            try
            {
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                write(stream);
            }
            catch
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // Left for SweepOldTempFiles.
                }
                throw;
            }

            return path;
        }

        /// <summary>
        /// A write-only pass-through that throws once <see cref="CancellationToken"/> is cancelled, for a write -
        /// DataTable.WriteXml - that takes no token of its own. Doesn't close the stream it writes to: that's the
        /// caller's.
        /// </summary>
        private sealed class CancellableStream(Stream inner, CancellationToken cancellationToken) : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                inner.Write(buffer, offset, count);
            }

            public override void Flush() => inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }

        private static void SweepOldTempFiles()
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddHours(-1);
                foreach (var pattern in new[] { "ssms_*.sqlplan", "ssms_*.xdl", "ssms_*.xml", "ssms_*.xml.gz" })
                {
                    foreach (var path in Directory.GetFiles(Path.GetTempPath(), pattern))
                    {
                        try
                        {
                            if (File.GetLastWriteTimeUtc(path) < cutoff) File.Delete(path);
                        }
                        catch
                        {
                            // Still open, or access denied - leave it for next time.
                        }
                    }
                }
            }
            catch
            {
                // Cleanup is not worth failing the launch over.
            }
        }

        /// <summary>
        /// Finds DBADashVisualizer.exe or DBADash.exe. The remembered path goes first, ahead of even the
        /// registered file associations: it's either exactly what the user browsed to explicitly, or -
        /// now that SsmsExtensionInstaller.Install() writes it too - exactly the app they just used to
        /// install this extension in the first place, which says more about which copy they actually
        /// mean than a possibly older, unrelated "Open with" registration does. Never stale enough to
        /// cause trouble: RememberedPath only returns it while the file is still there, so an uninstalled
        /// or moved copy just falls through to the checks below instead of being trusted anyway.
        ///
        /// PATH is checked last and deliberately: any writable directory earlier on it would otherwise
        /// let something else hijack the launch by dropping its own DBADashVisualizer.exe/DBADash.exe
        /// there. The registry locations ahead of it are either DBA Dash's own registration or something
        /// the user pointed at explicitly, so both are trusted first.
        /// </summary>
        private static string FindDBADashExe()
        {
            return RememberedPath()
                   ?? ExeFromProgIdCommand(VisualizerQueryPlanProgIdCommandKey)
                   ?? ExeFromProgIdCommand(VisualizerDeadlockProgIdCommandKey)
                   ?? ExeFromProgIdCommand(GuiQueryPlanProgIdCommandKey)
                   ?? ExeFromProgIdCommand(GuiDeadlockProgIdCommandKey)
                   ?? DefaultVelopackInstallPath()
                   ?? FindOnPath(VisualizerExeName)
                   ?? FindOnPath(GuiExeName);
        }

        private static string ExeFromProgIdCommand(string commandKeyPath)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(commandKeyPath);
                var command = key?.GetValue("") as string;
                var exePath = ExeFromCommandLine(command);
                return exePath != null && File.Exists(exePath) ? exePath : null;
            }
            catch
            {
                return null;
            }
        }

        private static string ExeFromCommandLine(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return null;
            command = command.Trim();
            if (!command.StartsWith("\"")) return command.Split(' ')[0];
            var end = command.IndexOf('"', 1);
            return end > 1 ? command.Substring(1, end - 1) : null;
        }

        /// <summary>Matches the packId/mainExe Pack-VisualizerInstaller.ps1 hands to Velopack. The full
        /// GUI has no equivalent well-known install path - it ships as a zip the user extracts wherever
        /// they like, so for that one there's only the registry and the remembered/browsed path.</summary>
        private static string DefaultVelopackInstallPath()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DBADash_Visualizer", "current", VisualizerExeName);
            return File.Exists(path) ? path : null;
        }

        private static string RememberedPath()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(OwnSettingsKey);
                var path = key?.GetValue(OwnSettingsValue) as string;
                return path != null && File.Exists(path) ? path : null;
            }
            catch
            {
                return null;
            }
        }

        private static string FindOnPath(string exeName)
        {
            var pathVar = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathVar)) return null;

            foreach (var dir in pathVar.Split(';'))
            {
                try
                {
                    var candidate = Path.Combine(dir.Trim(), exeName);
                    if (File.Exists(candidate)) return candidate;
                }
                catch
                {
                    // Malformed PATH entry.
                }
            }

            return null;
        }

        private static bool LaunchApp(string exePath, string tempFile)
        {
            RecordOwnVersion();

            try
            {
                // No single-instance forwarding needed here: both apps already forward a second launch
                // into a new tab of their own running window via ViewerInstance, so a plain Process.Start
                // gets that behaviour for free regardless of which one this is.
                Process.Start(new ProcessStartInfo(exePath, "\"" + tempFile + "\"")
                {
                    UseShellExecute = false
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Best effort: without it, the app just takes this for an extension older than the ones that record their
        /// version, and offers the update it carries - the same as for one that's genuinely out of date.
        /// </summary>
        private static void RecordOwnVersion()
        {
            try
            {
                if (OwnVersion is not { } version) return;
                using var key = Registry.CurrentUser.CreateSubKey(OwnSettingsKey);
                key?.SetValue(OwnVersionValue, version);
            }
            catch
            {
                // Best effort - see summary above.
            }
        }

        /// <summary>
        /// This extension's version: Identity/@Version from source.extension.vsixmanifest, embedded in this assembly
        /// by the .csproj - the one place the version is set, and the number SSMS and VSIXInstaller compare - rather
        /// than read from the installed extension's folder, whose layout is SSMS's business.
        /// </summary>
        private static string OwnVersion => _ownVersion.Value;

        private static readonly Lazy<string> _ownVersion = new(() =>
        {
            try
            {
                using var stream = typeof(VisualizerLauncher).Assembly.GetManifestResourceStream("source.extension.vsixmanifest");
                if (stream == null) return null;

                var manifest = new XmlDocument();
                manifest.Load(stream);
                var ns = new XmlNamespaceManager(manifest.NameTable);
                ns.AddNamespace("vsx", "http://schemas.microsoft.com/developer/vsx-schema/2011");
                return manifest.SelectSingleNode("/vsx:PackageManifest/vsx:Metadata/vsx:Identity/@Version", ns)?.Value;
            }
            catch
            {
                return null;
            }
        });

        /// <summary>Prompts for either exe once, and remembers the choice for next time.</summary>
        private static string BrowseForApp()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Locate DBA Dash Visualizer or DBA Dash",
                Filter = $"DBA Dash|{VisualizerExeName};{GuiExeName}|All executables|*.exe",
                FileName = VisualizerExeName
            };

            if (dialog.ShowDialog() != DialogResult.OK || !File.Exists(dialog.FileName)) return null;

            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(OwnSettingsKey);
                key?.SetValue(OwnSettingsValue, dialog.FileName);
            }
            catch
            {
                // Best effort - the launch this time still goes ahead either way.
            }

            return dialog.FileName;
        }
    }
}
