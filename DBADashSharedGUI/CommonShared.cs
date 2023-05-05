using DBADash;
using DBADashGUI;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace DBADashSharedGUI
{
    public class CommonShared
    {
        public static void OpenURL(string url)
        {
            var psi = new ProcessStartInfo(url) { UseShellExecute = true };
            Process.Start(psi);
        }

        [SupportedOSPlatform("windows")]
        public static void ShowAbout(IWin32Window owner, bool StartGUIOnUpgrade)
        {
            using About frm = new()
            {
                DBVersion = new Version(),
                StartGUIOnUpgrade = StartGUIOnUpgrade
            };
            frm.ShowDialog(owner);
        }

        [SupportedOSPlatform("windows")]
        public static void ShowAbout(string connectionString, IWin32Window owner, bool StartGUIOnUpgrade)
        {
            Version dbVersion= new Version();
            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    dbVersion = DBADash.DBValidations.GetDBVersion(connectionString);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(@"Error getting repository version: " + ex.Message, "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            using About frm = new()
            {
                DBVersion = dbVersion,
                StartGUIOnUpgrade = StartGUIOnUpgrade
            };
            frm.ShowDialog(owner);
        }

        public static void StyleGrid(ref DataGridView dgv)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.GetType() == typeof(DataGridViewLinkColumn))
                {
                    var linkCol = (DataGridViewLinkColumn)col;
                    linkCol.LinkColor = DashColors.LinkColor;
                }
            }
        }

        public static async Task CheckForIncompleteUpgrade()
        {
            if (!DBADash.Upgrade.IsUpgradeIncomplete) return;

            MessageBox.Show(DBADash.Upgrade.IncompleteUpgradeMessage, "Error", MessageBoxButtons.OK,
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