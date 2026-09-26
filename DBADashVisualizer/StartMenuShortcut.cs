using Serilog;
using System.IO;
using System.Runtime.InteropServices;

namespace DBADashVisualizer
{
    /// <summary>
    /// A shortcut to the app in the current user's Start menu.
    ///
    /// The app does this itself - when the user asks - rather than an installer, because a zip and a winget portable
    /// package have nothing else to do it.  Per user, so it needs no elevation, and there is one shortcut however many copies
    /// of the app are on the machine: it points at whichever copy created it last.
    /// </summary>
    internal static class StartMenuShortcut
    {
        private static string ShortcutPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Programs), "DBA Dash Visualizer.lnk");

        private static string ExePath => Environment.ProcessPath ?? Application.ExecutablePath;

        /// <summary>True when there is a shortcut, wherever it points.</summary>
        internal static bool Exists => File.Exists(ShortcutPath);

        /// <summary>True when the shortcut is there and points at this copy.</summary>
        internal static bool PointsAtThisCopy => Exists && SamePath(Target(), ExePath);

        /// <summary>Creates the shortcut, or points the one that is there at this copy.</summary>
        internal static void Create()
        {
            var shell = CreateShell();
            try
            {
                dynamic shortcut = shell.CreateShortcut(ShortcutPath);
                try
                {
                    shortcut.TargetPath = ExePath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(ExePath);
                    shortcut.IconLocation = ExePath + ",0";
                    shortcut.Description = "View SQL Server execution plans and deadlock graphs";
                    shortcut.Save();
                }
                finally
                {
                    Marshal.ReleaseComObject(shortcut);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }
        }

        /// <summary>Removes the shortcut.  Nothing to do if there isn't one.</summary>
        internal static void Remove()
        {
            if (Exists) File.Delete(ShortcutPath);
        }

        /// <summary>
        /// Called at start-up.  Only keeps an existing shortcut working - it never adds one the user didn't ask for, and it
        /// leaves one that points at another copy that is still there.  Points it at this copy when the copy it pointed at has
        /// gone, typically the folder an earlier version was unzipped to.
        /// </summary>
        internal static void Repair()
        {
            try
            {
                if (!Exists) return;

                var target = Target();
                if (string.IsNullOrWhiteSpace(target) || SamePath(target, ExePath) || File.Exists(target)) return;

                Log.Information("Pointing the Start menu shortcut at {path}.  Previously {previous}", ExePath, target);
                Create();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to repair the Start menu shortcut");
            }
        }

        private static string Target()
        {
            var shell = CreateShell();
            try
            {
                dynamic shortcut = shell.CreateShortcut(ShortcutPath);
                try
                {
                    return shortcut.TargetPath as string;
                }
                finally
                {
                    Marshal.ReleaseComObject(shortcut);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }
        }

        // The Windows Script Host's shell object - the simplest way to read and write a .lnk from .NET.
        private static dynamic CreateShell() =>
            Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")
                                     ?? throw new InvalidOperationException("Windows Script Host isn't available.")) ??
            throw new InvalidOperationException("Unable to create the shell object.");

        private static bool SamePath(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            try
            {
                return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
