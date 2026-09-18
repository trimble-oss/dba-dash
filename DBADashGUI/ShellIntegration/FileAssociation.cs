using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DBADashGUI.ShellIntegration
{
    /// <summary>
    /// Registers DBA Dash as a handler for the file types it has a viewer for, so one can be opened
    /// from Explorer - Open with, or a double click once DBA Dash is the default - straight into the
    /// right viewer.
    ///
    /// Registration is per user under HKCU: DBA Dash ships as a zip rather than an installer, so
    /// there is nothing else to do it, and it needs no elevation.  It is only done when the user
    /// asks - from a viewer's settings menu or the command line - never just because DBA Dash was
    /// started.  It only makes DBA Dash one of the applications offered; Windows does not let an
    /// application make itself the default (the user's choice is protected by a hash), so that is
    /// left to the user in Default Apps.
    ///
    /// There is one registration however many copies of DBA Dash are on the machine: two entries
    /// both called "DBA Dash" in Open with could not be told apart, and every copy unzipped for an
    /// upgrade would leave one behind.  Whichever copy registered last holds it - the user can move
    /// it to another copy from a viewer's settings menu - but at startup a copy takes it over when
    /// the registered exe has gone away or is an older version.  That includes a copy the user
    /// picked explicitly, and a newer dev build.
    ///
    /// One class for every file type rather than one per type, because the registry keys are not all
    /// per type.  The Capabilities key, the RegisteredApplications entry and the exe's SupportedTypes
    /// list are shared by every type DBA Dash handles - so a per type class would have each of them
    /// deleting the others' entries on unregister, and the bug would only show up once a second
    /// viewer existed.
    /// </summary>
    internal sealed class FileAssociation
    {
        /// <summary>SQL Server deadlock graphs, opened in the deadlock viewer.</summary>
        public static readonly FileAssociation DeadlockGraph = new(
            ".xdl",
            "DBADash.DeadlockGraph",
            "SQL Server Deadlock Graph");

        /// <summary>SQL Server execution plans, opened in the query plan viewer.</summary>
        public static readonly FileAssociation QueryPlan = new(
            ".sqlplan",
            "DBADash.QueryPlan",
            "SQL Server Execution Plan");

        /// <summary>Every type DBA Dash offers to handle.</summary>
        public static readonly IReadOnlyList<FileAssociation> All = [DeadlockGraph, QueryPlan];

        /// <summary>The name under RegisteredApplications - also what Default Apps is asked to navigate to.</summary>
        private const string RegisteredAppName = "DBADash";

        private const string ClassesKey = @"Software\Classes";
        private const string AppKey = @"Software\DBADash";
        private const string CapabilitiesKey = AppKey + @"\Capabilities";
        private const string RegisteredApplicationsKey = @"Software\RegisteredApplications";

        private FileAssociation(string extension, string progId, string typeName)
        {
            Extension = extension;
            ProgId = progId;
            TypeName = typeName;
        }

        /// <summary>The extension, with its leading dot - e.g. ".xdl".</summary>
        public string Extension { get; }

        private string ProgId { get; }

        private string TypeName { get; }

        private static string ExePath => Environment.ProcessPath ?? Application.ExecutablePath;

        private static string ExeName => Path.GetFileName(ExePath);

        private static string OpenCommand => $"\"{ExePath}\" \"%1\"";

        private static string ApplicationKey => $@"{ClassesKey}\Applications\{ExeName}";

        private string ProgIdCommandKey => $@"{ClassesKey}\{ProgId}\shell\open\command";

        private string OpenWithProgIdsKey => $@"{ClassesKey}\{Extension}\OpenWithProgids";

        /// <summary>
        /// The exe this type is registered to open in, from whichever copy registered.  Null if none.
        /// </summary>
        public string RegisteredExePath
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(ProgIdCommandKey);
                return ExeFromCommand(key?.GetValue("") as string);
            }
        }

        /// <summary>True when the registration is for this copy of DBA Dash.</summary>
        public bool IsRegisteredToThisCopy => SamePath(RegisteredExePath, ExePath);

        /// <summary>
        /// True when files of this type currently open in this copy of DBA Dash - the user made it
        /// the default.
        /// </summary>
        public bool IsDefaultHandler => SamePath(AssocQueryExecutable(Extension), ExePath);

        /// <summary>
        /// Called at startup, for every type.  Only keeps existing registrations working - it never
        /// adds one the user did not ask for.  Moves one to this copy when the registered copy has
        /// since been deleted (typically the old folder after an upgrade) or is an older version.
        /// </summary>
        public static void UpdateRegistrations()
        {
            foreach (var association in All) association.UpdateRegistration();
        }

        private void UpdateRegistration()
        {
            try
            {
                var registered = RegisteredExePath;
                if (registered == null || SamePath(registered, ExePath)) return;

                // An upgrade is often unzipped alongside the old version rather than over it - the
                // newer copy takes over so files do not keep opening in the old viewer.
                if (File.Exists(registered) && !IsNewerThan(registered)) return;

                Log.Information("Registering {path} to open {extension} files.  Previously {previous}",
                    ExePath, Extension, registered);
                Register();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to register the {extension} file association", Extension);
            }
        }

        /// <summary>Registers this copy for every type, taking over from any other.</summary>
        public static void RegisterAll()
        {
            foreach (var association in All) association.Register();
        }

        /// <summary>Removes the registration for every type.</summary>
        public static void UnregisterAll()
        {
            foreach (var association in All) association.Unregister();
        }

        /// <summary>Registers this copy for this type, taking over from any other.</summary>
        public void Register()
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

            // Windows keys Open with entries picked by browsing to the exe on its file name.
            // Without a FriendlyAppName that entry reads as the file description, "DBA Dash GUI".
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
                capabilities.SetValue("ApplicationDescription",
                    "SQL Server monitoring - includes viewers for deadlock graphs (.xdl) and execution plans (.sqlplan)");
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
        /// Removes what <see cref="Register"/> added for this type.  The user's own default choice,
        /// if they picked DBA Dash, is Windows' to keep - with the ProgID gone it simply falls back
        /// to asking.
        ///
        /// The keys shared with the other types - Capabilities, RegisteredApplications, the exe's
        /// SupportedTypes - lose only this type's entry, and are removed altogether only once no
        /// type is registered any more.
        /// </summary>
        public void Unregister()
        {
            var user = Registry.CurrentUser;

            user.DeleteSubKeyTree($@"{ClassesKey}\{ProgId}", false);

            using (var openWith = user.OpenSubKey(OpenWithProgIdsKey, true))
            {
                openWith?.DeleteValue(ProgId, false);
            }

            using (var supported = user.OpenSubKey($@"{ApplicationKey}\SupportedTypes", true))
            {
                supported?.DeleteValue(Extension, false);
            }

            using (var associations = user.OpenSubKey($@"{CapabilitiesKey}\FileAssociations", true))
            {
                associations?.DeleteValue(Extension, false);
            }

            // Anything still registered keeps the shared keys alive.
            if (All.Any(a => a.RegisteredExePath != null))
            {
                NotifyShell();
                return;
            }

            user.DeleteSubKeyTree(ApplicationKey, false);
            user.DeleteSubKeyTree(CapabilitiesKey, false);

            using (var registered = user.OpenSubKey(RegisteredApplicationsKey, true))
            {
                registered?.DeleteValue(RegisteredAppName, false);
            }

            // Software\DBADash only holds the capabilities - do not leave it behind empty.
            bool appKeyEmpty;
            using (var app = user.OpenSubKey(AppKey))
            {
                appKeyEmpty = app is { SubKeyCount: 0, ValueCount: 0 };
            }

            if (appKeyEmpty) user.DeleteSubKey(AppKey, false);

            NotifyShell();
        }

        /// <summary>
        /// Opens Settings > Default apps, on the DBA Dash page where Windows supports it, so the
        /// user can make DBA Dash the default.  Registers first, as DBA Dash is not listed otherwise.
        /// </summary>
        public void OpenDefaultAppsSettings()
        {
            if (!IsRegisteredToThisCopy) Register();
            Process.Start(new ProcessStartInfo($"ms-settings:defaultapps?registeredAppUser={RegisteredAppName}")
            { UseShellExecute = true })?.Dispose();
        }

        /// <summary>
        /// True for a handler that is this copy of DBA Dash - no use offering to open a file in
        /// itself.
        /// </summary>
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

        private static string AssocQueryExecutable(string extension)
        {
            uint length = 0;
            // S_FALSE (1) with the length needed
            if (AssocQueryString(ASSOCF_NOTRUNCATE, ASSOCSTR_EXECUTABLE, extension, "open", null, ref length) != 1 ||
                length == 0)
            {
                return null;
            }

            var sb = new StringBuilder((int)length);
            return AssocQueryString(ASSOCF_NOTRUNCATE, ASSOCSTR_EXECUTABLE, extension, "open", sb, ref length) == 0
                ? sb.ToString()
                : null;
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
