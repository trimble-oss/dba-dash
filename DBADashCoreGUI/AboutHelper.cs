using DBADash;
using DBADashGUI;
using DBADashSharedGUI;
using System.Runtime.Versioning;

namespace DBADashCoreGUI
{
    /// <summary>
    /// The About dialog and upgrade prompts, shared by the GUI and the service config tool.
    /// </summary>
    public static class AboutHelper
    {
        [SupportedOSPlatform("windows")]
        public static void ShowAbout(IWin32Window owner, bool StartGUIOnUpgrade, bool includePreRelease = false)
        {
            using About frm = new()
            {
                DBVersion = new Version(),
                StartGUIOnUpgrade = StartGUIOnUpgrade,
                IncludePreRelease = includePreRelease
            };
            frm.ShowDialog(owner);
        }

        [SupportedOSPlatform("windows")]
        public static void ShowAbout(string connectionString, IWin32Window owner, bool StartGUIOnUpgrade, bool includePreRelease = false)
        {
            Version dbVersion = new();
            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    dbVersion = DBValidations.GetDBVersion(connectionString).Version;
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, @"Error getting repository version");
                }
            }
            using About frm = new()
            {
                DBVersion = dbVersion,
                StartGUIOnUpgrade = StartGUIOnUpgrade,
                IncludePreRelease = includePreRelease
            };
            frm.ShowDialog(owner);
        }

        [SupportedOSPlatform("windows")]
        public static async Task CheckForIncompleteUpgrade()
        {
            if (!Upgrade.IsUpgradeIncomplete) return;

            MessageBox.Show(Upgrade.IncompleteUpgradeMessage, "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            if (MessageBox.Show("Retry upgrade?", "Retry", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                await Upgrade.UpgradeDBADashAsync();
            }

            Application.Exit();
        }
    }
}
