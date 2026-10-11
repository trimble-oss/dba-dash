using System.IO;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// Whether a copy installed by Velopack is for the current user or all users.  The setup program and the .msi's "just me"
    /// install under the user's LocalAppData; the .msi's "everyone" installs under Program Files (or wherever an admin chose),
    /// and installing an update to it asks for administrator permission and updates it for every user of the machine.
    /// </summary>
    public static class InstallScope
    {
        /// <summary>True when <paramref name="installedPath"/> - the app's folder, in Velopack's install folder - isn't in the user's own LocalAppData.</summary>
        public static bool IsForAllUsers(string installedPath)
        {
            if (string.IsNullOrEmpty(installedPath)) return false;
            var local = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
                .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return !Path.GetFullPath(installedPath).StartsWith(local, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Said where an update is offered to a copy installed for all users.</summary>
        public const string AdminNote = "It is installed for all users, so you will be asked for administrator permission.";
    }
}
