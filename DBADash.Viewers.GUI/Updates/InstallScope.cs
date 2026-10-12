using Microsoft.Win32;
using System.IO;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// Whether a copy installed by Velopack is for the current user or all users.  Only the .msi's "everyone" install is for all
    /// users, and it registers its Installed apps entry, "MSI:&lt;packId&gt;", in HKLM - the setup program and the .msi's "just me"
    /// register in HKCU, wherever the files went.  Installing an update to a copy for all users asks for administrator permission
    /// and updates it for every user of the machine.
    /// </summary>
    public static class InstallScope
    {
        /// <summary>True when <paramref name="installedPath"/> - the app's folder, in Velopack's install folder - is in the install
        /// for all users of <paramref name="packId"/>.</summary>
        public static bool IsForAllUsers(string installedPath, string packId) =>
            !string.IsNullOrEmpty(packId) && IsIn(installedPath, MachineInstallLocation(packId));

        /// <summary>True when <paramref name="installedPath"/> is in <paramref name="machineInstallLocation"/>, the folder the
        /// install for all users recorded (null when there isn't one).</summary>
        public static bool IsIn(string installedPath, string machineInstallLocation)
        {
            if (string.IsNullOrEmpty(installedPath) || string.IsNullOrWhiteSpace(machineInstallLocation)) return false;
            try
            {
                var root = Path.GetFullPath(machineInstallLocation).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return Path.GetFullPath(installedPath).StartsWith(root, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string MachineInstallLocation(string packId)
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var key = root.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Uninstall\MSI:{packId}");
                return key?.GetValue("InstallLocation") as string;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Said where an update is offered to a copy installed for all users.</summary>
        public const string AdminNote = "It is installed for all users, so you will be asked for administrator permission.";
    }
}
