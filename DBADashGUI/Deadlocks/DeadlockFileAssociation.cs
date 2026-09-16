using Microsoft.Win32;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// Registers DBA Dash as a handler for .xdl files, so a deadlock graph can be opened from Explorer - Open with,
    /// or a double click once DBA Dash is the default - straight into the deadlock viewer.
    ///
    /// Registration is per user under HKCU: DBA Dash ships as a zip rather than an installer, so there is nothing
    /// else to do it, and it needs no elevation.  It is only done when the user asks - from the deadlock viewer's
    /// settings menu or the command line - never just because DBA Dash was started.  It only makes DBA Dash one of
    /// the applications offered - Windows doesn't let an application make itself the default (the user's choice is
    /// protected by a hash), so that is left to the user in Default Apps.
    ///
    /// There is one registration however many copies of DBA Dash are on the machine: two entries both called
    /// "DBA Dash" in Open with couldn't be told apart, and every copy unzipped for an upgrade would leave one
    /// behind.  Whichever copy registered last holds it - the user can move it to another copy from the viewer's
    /// settings menu - but at startup a copy takes it over when the registered exe has gone away or is an older
    /// version.  That includes a copy the user picked explicitly, and a newer dev build.
    /// </summary>
    internal static class DeadlockFileAssociation
    {
        public const string Extension = ".xdl";

        private const string ProgId = "DBADash.DeadlockGraph";
        private const string TypeName = "SQL Server Deadlock Graph";

        /// <summary>The name under RegisteredApplications - also what Default Apps is asked to navigate to.</summary>
        private const string RegisteredAppName = "DBADash";

        private const string ClassesKey = @"Software\Classes";
        private const string AppKey = @"Software\DBADash";
        private const string CapabilitiesKey = AppKey + @"\Capabilities";
        private const string RegisteredApplicationsKey = @"Software\RegisteredApplications";

        private static string ExePath => Environment.ProcessPath ?? Application.ExecutablePath;

        private static string ExeName => Path.GetFileName(ExePath);

        private static string OpenCommand => $"\"{ExePath}\" \"%1\"";

        private static string ProgIdCommandKey => $@"{ClassesKey}\{ProgId}\shell\open\command";

        private static string OpenWithProgIdsKey => $@"{ClassesKey}\{Extension}\OpenWithProgids";

        private static string ApplicationKey => $@"{ClassesKey}\Applications\{ExeName}";

        /// <summary>The exe .xdl files are registered to open in, from whichever copy registered.  Null if none.</summary>
        public static string RegisteredExePath
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(ProgIdCommandKey);
                return ExeFromCommand(key?.GetValue("") as string);
            }
        }

        /// <summary>True when the registration is for this copy of DBA Dash.</summary>
        public static bool IsRegisteredToThisCopy => SamePath(RegisteredExePath, ExePath);

        /// <summary>True when .xdl files currently open in this copy of DBA Dash - the user made it the default.</summary>
        public static bool IsDefaultHandler => SamePath(AssocQueryExecutable(), ExePath);

        /// <summary>
        /// Called at startup.  Only keeps an existing registration working - it never adds one the user didn't ask
        /// for.  Moves it to this copy when the registered copy has since been deleted (typically the old folder
        /// after an upgrade) or is an older version.
        /// </summary>
        public static void UpdateRegistration()
        {
            try
            {
                var registered = RegisteredExePath;
                if (registered == null || SamePath(registered, ExePath)) return;
                // An upgrade is often unzipped alongside the old version rather than over it - the newer copy takes
                // over so graphs don't keep opening in the old viewer.
                if (File.Exists(registered) && !IsNewerThan(registered)) return;

                Log.Information("Registering {path} to open deadlock graph (.xdl) files.  Previously {previous}", ExePath, registered);
                Register();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to register the deadlock graph file association");
            }
        }

        /// <summary>Registers this copy, taking over from any other.</summary>
        public static void Register()
        {
            var user = Registry.CurrentUser;

            using (var progId = user.CreateSubKey($@"{ClassesKey}\{ProgId}"))
            {
                progId.SetValue("", TypeName);
                progId.SetValue("FriendlyTypeName", TypeName);
                using (var icon = progId.CreateSubKey("DefaultIcon"))
                {
                    icon.SetValue("", $"\"{ExePath}\",0");
                }
            }

            using (var command = user.CreateSubKey(ProgIdCommandKey))
            {
                command.SetValue("", OpenCommand);
            }

            using (var openWith = user.CreateSubKey(OpenWithProgIdsKey))
            {
                openWith.SetValue(ProgId, "");
            }

            // Windows keys Open with entries picked by browsing to the exe on its file name.  Without a
            // FriendlyAppName that entry reads as the file description, "DBA Dash GUI".
            using (var app = user.CreateSubKey(ApplicationKey))
            {
                app.SetValue("FriendlyAppName", "DBA Dash");
                using (var supported = app.CreateSubKey("SupportedTypes"))
                {
                    supported.SetValue(Extension, "");
                }
                using (var command = app.CreateSubKey(@"shell\open\command"))
                {
                    command.SetValue("", OpenCommand);
                }
            }

            // Capabilities + RegisteredApplications are what list DBA Dash in Settings > Default apps.
            using (var capabilities = user.CreateSubKey(CapabilitiesKey))
            {
                capabilities.SetValue("ApplicationName", "DBA Dash");
                capabilities.SetValue("ApplicationDescription", "SQL Server monitoring - includes a viewer for deadlock graphs (.xdl)");
                using (var associations = capabilities.CreateSubKey("FileAssociations"))
                {
                    associations.SetValue(Extension, ProgId);
                }
            }

            using (var registered = user.CreateSubKey(RegisteredApplicationsKey))
            {
                registered.SetValue(RegisteredAppName, CapabilitiesKey);
            }

            NotifyShell();
        }

        /// <summary>
        /// Removes everything <see cref="Register"/> added.  The user's own default choice, if they picked DBA
        /// Dash, is Windows' to keep - with the ProgID gone it simply falls back to asking.
        /// </summary>
        public static void Unregister()
        {
            var user = Registry.CurrentUser;

            user.DeleteSubKeyTree($@"{ClassesKey}\{ProgId}", false);
            user.DeleteSubKeyTree(ApplicationKey, false);
            user.DeleteSubKeyTree(CapabilitiesKey, false);

            using (var openWith = user.OpenSubKey(OpenWithProgIdsKey, true))
            {
                openWith?.DeleteValue(ProgId, false);
            }

            using (var registered = user.OpenSubKey(RegisteredApplicationsKey, true))
            {
                registered?.DeleteValue(RegisteredAppName, false);
            }

            // Software\DBADash only holds the capabilities - don't leave it behind empty.
            bool appKeyEmpty;
            using (var app = user.OpenSubKey(AppKey))
            {
                appKeyEmpty = app is { SubKeyCount: 0, ValueCount: 0 };
            }
            if (appKeyEmpty) user.DeleteSubKey(AppKey, false);

            NotifyShell();
        }

        /// <summary>
        /// Opens Settings > Default apps, on the DBA Dash page where Windows supports it, so the user can make
        /// DBA Dash the default for .xdl.  Registers first, as DBA Dash isn't listed otherwise.
        /// </summary>
        public static void OpenDefaultAppsSettings()
        {
            if (!IsRegisteredToThisCopy) Register();
            Process.Start(new ProcessStartInfo($"ms-settings:defaultapps?registeredAppUser={RegisteredAppName}") { UseShellExecute = true })?.Dispose();
        }

        /// <summary>True for a handler that is this copy of DBA Dash - no use offering to open a graph in itself.</summary>
        public static bool IsThisCopy(string exePath) => SamePath(exePath, ExePath);

        /// <summary>True when this copy is a later version than the exe at <paramref name="otherExePath"/>.</summary>
        private static bool IsNewerThan(string otherExePath)
        {
            try
            {
                return FileVersion(ExePath) > FileVersion(otherExePath);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Unable to compare the version of {path}", otherExePath);
                return false;
            }
        }

        private static Version FileVersion(string exePath)
        {
            var info = FileVersionInfo.GetVersionInfo(exePath);
            return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
        }

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

        private static string ExeFromCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return null;
            command = command.Trim();
            if (!command.StartsWith('"')) return command.Split(' ')[0];
            var end = command.IndexOf('"', 1);
            return end > 1 ? command[1..end] : null;
        }

        private static void NotifyShell() => SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

        private static string AssocQueryExecutable()
        {
            uint length = 0;
            // S_FALSE (1) with the length needed
            if (AssocQueryString(ASSOCF_NOTRUNCATE, ASSOCSTR_EXECUTABLE, Extension, "open", null, ref length) != 1 || length == 0) return null;
            var sb = new StringBuilder((int)length);
            return AssocQueryString(ASSOCF_NOTRUNCATE, ASSOCSTR_EXECUTABLE, Extension, "open", sb, ref length) == 0 ? sb.ToString() : null;
        }

        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;
        private const uint ASSOCF_NOTRUNCATE = 0x20;
        private const int ASSOCSTR_EXECUTABLE = 2;

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int AssocQueryString(uint flags, int str, string pszAssoc, string pszExtra,
            StringBuilder pszOut, ref uint pcchOut);
    }
}
